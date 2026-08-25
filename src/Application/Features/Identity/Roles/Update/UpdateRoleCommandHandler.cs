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
    public async Task<Result> HandleAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(command.Id);
        if (role is null)
            return Result.Failure(Error.NotFound("Role.NotFound", $"Role with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Role.Forbidden", "You do not have permission to update roles."));

        if (!string.Equals(role.Name, command.Name, StringComparison.OrdinalIgnoreCase))
        {
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
