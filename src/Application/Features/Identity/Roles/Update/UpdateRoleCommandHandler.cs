namespace ZARI.Application.Features.Identity.Roles.Update;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<UpdateRoleCommand>
{
    /// <summary>Seeded via AppDbSeeder.SeedRolesAsync — never renamable, mirrors DeleteRoleCommandHandler's guard (permissions on these roles can still be edited, just not the name).</summary>
    private static readonly HashSet<string> SystemRoleNames = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "Staff" };

    public async Task<Result> HandleAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(command.Id);
        if (role is null)
            return Result.Failure(Error.NotFound("Role.NotFound", $"Role with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Role.Forbidden", "You do not have permission to update roles."));

        if (!string.Equals(role.Name, command.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (SystemRoleNames.Contains(role.Name!))
                return Result.Failure(Error.Conflict("Role.SystemRole", $"'{role.Name}' is a built-in system role and cannot be renamed."));

            var nameOwner = await roleManager.FindByNameAsync(command.Name);
            if (nameOwner is not null && nameOwner.Id != role.Id)
                return Result.Failure(Error.Conflict("Role.DuplicateName", $"A role named '{command.Name}' already exists."));
        }

        var formCodes = command.Permissions.Select(p => p.FormCode).Distinct().ToList();
        var formCount = await dbContext.Forms.CountAsync(f => formCodes.Contains(f.Code), cancellationToken);
        if (formCount != formCodes.Count)
            return Result.Failure(Error.NotFound("Form.NotFound", "One or more forms were not found."));

        await roleManager.SetRoleNameAsync(role, command.Name);
        var updateResult = await roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Role.UpdateFailed", errors));
        }

        await dbContext.RolePermissions.Where(rp => rp.RoleId == role.Id).ExecuteDeleteAsync(cancellationToken);

        // The permission check above (HasPermissionAsync) tracks this same role's RolePermission
        // rows when the acting user holds the role being edited (e.g. Admin editing Admin) — those
        // rows are now stale (ExecuteDeleteAsync bypasses the tracker, so it doesn't know they're
        // gone) and would collide with the new instances added below, which share the same
        // (RoleId, FormCode) key. Clear them out first.
        dbContext.ChangeTracker.Clear();

        var newPermissions = command.Permissions.Select(p => new RolePermission
        {
            RoleId = role.Id,
            FormCode = p.FormCode,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanApprove = p.CanApprove,
            CanCancel = p.CanCancel,
            CanDelete = p.CanDelete
        });

        dbContext.RolePermissions.AddRange(newPermissions);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
