namespace ZARI.Infrastructure.Identity;

using System.Security.Claims;
using ZARI.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // TokenService only ever puts the user's id in a custom "uid" claim (plus "sub" = username) —
    // with MapInboundClaims = false on the JWT bearer options, ClaimTypes.NameIdentifier is never
    // populated for a real issued token, so reading it here always resolved to null.
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue("uid");
    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
