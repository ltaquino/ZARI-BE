namespace ZARI.Application.Features.Identity.Roles.Delete;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService) : ICommandHandler<DeleteRoleCommand>
{
    /// <summary>Seeded via AppDbSeeder.SeedRolesAsync — never deletable, regardless of who holds ROLES:CanDelete.</summary>
    private static readonly HashSet<string> SystemRoleNames = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "Staff" };

    public async Task<Result> HandleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(command.Id);
        if (role is null)
            return Result.Failure(Error.NotFound("Role.NotFound", $"Role with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Role.Forbidden", "You do not have permission to delete roles."));

        if (SystemRoleNames.Contains(role.Name!))
            return Result.Failure(Error.Conflict("Role.SystemRole", $"'{role.Name}' is a built-in system role and cannot be deleted."));

        var assignedUsers = await userManager.GetUsersInRoleAsync(role.Name!);
        if (assignedUsers.Count > 0)
            return Result.Failure(Error.Conflict("Role.HasUsers", $"Cannot delete this role — it is assigned to {assignedUsers.Count} user{(assignedUsers.Count == 1 ? "" : "s")}."));

        var deleteResult = await roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Failure("Role.DeleteFailed", errors));
        }

        return Result.Success();
    }
}
