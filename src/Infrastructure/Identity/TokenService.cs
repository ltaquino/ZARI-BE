using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.DTOs.Identity;
using ZARI.Domain.Entities;

namespace ZARI.Infrastructure.Identity;

public sealed class TokenService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : ITokenService
{
    public async Task<TokenResponse> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var userClaims = await userManager.GetClaimsAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = new List<Claim>();
        for (int i = 0; i < roles.Count; i++)
        {
            roleClaims.Add(new Claim("roles", roles[i]));
        }
        var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("uid", user.Id),
                new Claim("first_name", user.FirstName),
                new Claim("last_name", user.LastName),
                new Claim("full_name", $"{user.FirstName} {user.LastName}"),

            }
            .Union(userClaims)
            .Union(roleClaims);

        Console.WriteLine("Being checked...");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // 1. Keep these strictly as pure UtcNow for the structural JWT token payload
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(double.Parse(configuration["Jwt:ExpirationInMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            notBefore: issuedAt, // Recommended: explicitly set the NotBefore claim
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(double.Parse(configuration["Jwt:RefreshTokenExpirationInDays"]!));
        await userManager.UpdateAsync(user);

        TokenResponse tokenResponse = new TokenResponse();
        tokenResponse.Id = user.Id;
        tokenResponse.Email = user.Email!;
        tokenResponse.JWToken = accessToken;

        // 2. FIX: Adjust your display values here for your response client objects if required
        tokenResponse.IssuedOn = issuedAt.AddHours(8);
        tokenResponse.ExpiresOn = expiresAt.AddHours(8);

        tokenResponse.UserName = user.UserName!;
        tokenResponse.FirstName = user.FirstName;
        tokenResponse.LastName = user.LastName;
        tokenResponse.Roles = roles.ToList();
        tokenResponse.IsVerified = user.EmailConfirmed;
        tokenResponse.RefreshToken = refreshToken;

        return tokenResponse;
    }

    public async Task<TokenResponse> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);

        // FIX: Look for JwtRegisteredClaimNames.Sub ("sub") instead of ClaimTypes.NameIdentifier
        var userName = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? throw new InvalidOperationException("Invalid access token.");

        var user = await userManager.FindByNameAsync(userName) // Find by UserName instead of Id
                   ?? throw new InvalidOperationException("User not found.");

        if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Invalid or expired refresh token.");

        return await GenerateTokenAsync(user, cancellationToken);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!))
        };

        // MapInboundClaims defaults to true on a bare JwtSecurityTokenHandler, which would silently
        // rewrite "sub" to ClaimTypes.NameIdentifier before we ever get to read it below — matches
        // the MapInboundClaims = false already set on the JwtBearer options used for live requests.
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        ClaimsPrincipal principal;
        SecurityToken securityToken;
        try
        {
            principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            // A malformed, re-signed, or otherwise unparseable access token — not a caller bug, so
            // surface it the same way as every other refresh-rejection path (InvalidOperationException,
            // which RefreshTokenCommandHandler maps to a clean 400) instead of letting it bubble up
            // as an unhandled 500.
            throw new InvalidOperationException("Invalid access token.", ex);
        }

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new InvalidOperationException("Invalid token.");

        return principal;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
