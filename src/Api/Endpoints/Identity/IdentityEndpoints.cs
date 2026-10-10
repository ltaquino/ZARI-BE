namespace ZARI.Api.Endpoints;

using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.DTOs.Identity;
using ZARI.Application.Features.Identity.Login;
using ZARI.Application.Features.Identity.RefreshToken;
using ZARI.Domain.Common;

public static class IdentityEndpoints
{
    // SECURITY FIX (found during production-readiness UAT, 2026-10-03): this group used to also map
    // an anonymous POST /register that took a free-text "Roles" string straight from the client body
    // with NO permission check -- anyone with network access could register with "Roles":"Admin" and
    // immediately log in as a full administrator. The only real, authorized user-provisioning path in
    // this system is the permission-checked POST /api/users (see UserEndpoints.cs / CreateUserCommand
    // Handler, which resolves roles by id and requires USERS:Create). The public self-register endpoint
    // was unused dead template code (confirmed zero references anywhere in the frontend) and has been
    // removed entirely rather than patched, to close the attack surface completely.
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity")
            .WithTags("Identity")
            .WithGroupName("Identity");

        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .RequireRateLimiting("login")
            .WithName("Login")
            .WithSummary("Login with email and password");

        group.MapPost("/refresh", Refresh)
            .RequireRateLimiting("login")
            .WithName("RefreshToken")
            .WithSummary("Refresh an expired access token");
    }

    private static async Task<IResult> Login(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommand command,
        ICommandHandler<RefreshTokenCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
