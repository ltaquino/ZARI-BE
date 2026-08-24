namespace ZARI.Application.Abstractions.Identity;

using ZARI.Application.DTOs.Identity;
using ZARI.Domain.Entities;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}
