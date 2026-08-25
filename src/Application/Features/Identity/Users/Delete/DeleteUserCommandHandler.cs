namespace ZARI.Application.Features.Identity.Users.Delete;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteUserCommandHandler(UserManager<ApplicationUser> userManager, IPermissionService permissionService) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.Id);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("USERS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("User.Forbidden", "You do not have permission to delete users."));

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Failure("User.DeleteFailed", errors));
        }

        return Result.Success();
    }
}
