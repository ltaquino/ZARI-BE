namespace ZARI.Application.Features.Identity.Users.ResetPassword;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PRODUCTION-READINESS FIX (found during UAT, 2026-10-03): before this, there was NO way for
/// anyone -- self-service or admin -- to change or reset a user's password after account creation.
/// CreateUserCommandHandler is the only place a password was ever set; UpdateUserCommandHandler
/// never touches it. A real cooperative's staff locking themselves out had no recourse short of
/// direct database manipulation. This is the minimum viable fix: an authorized admin can force-set
/// a new password. There's no SMTP configured anywhere in this system yet (see appsettings.json),
/// so a proper self-service "forgot password" email flow isn't possible until that's wired up --
/// that should be a follow-up once it is. Also clears the user's refresh token, so a reset
/// immediately invalidates any session someone else may have been using on that account.
/// </summary>
public sealed class ResetUserPasswordCommandHandler(UserManager<ApplicationUser> userManager, IPermissionService permissionService)
    : ICommandHandler<ResetUserPasswordCommand>
{
    public async Task<Result> HandleAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("USERS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("User.Forbidden", "You do not have permission to reset user passwords."));

        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{command.UserId}' was not found."));

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("User.ResetPasswordFailed", errors));
        }

        var addResult = await userManager.AddPasswordAsync(user, command.NewPassword);
        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("User.ResetPasswordFailed", errors));
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await userManager.UpdateAsync(user);

        return Result.Success();
    }
}
