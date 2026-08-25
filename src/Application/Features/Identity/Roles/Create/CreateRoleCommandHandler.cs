namespace ZARI.Application.Features.Identity.Roles.Create;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.Create, cancellationToken))
            return Result.Failure<RoleResponse>(Error.Forbidden("Role.Forbidden", "You do not have permission to create roles."));

        if (await roleManager.RoleExistsAsync(command.Name))
            return Result.Failure<RoleResponse>(Error.Conflict("Role.DuplicateName", $"A role named '{command.Name}' already exists."));

        var formCodes = command.Permissions.Select(p => p.FormCode).Distinct().ToList();
        var formCount = await dbContext.Forms.CountAsync(f => formCodes.Contains(f.Code), cancellationToken);
        if (formCount != formCodes.Count)
            return Result.Failure<RoleResponse>(Error.NotFound("Form.NotFound", "One or more forms were not found."));

        var role = new IdentityRole(command.Name);
        var createResult = await roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<RoleResponse>(Error.Validation("Role.CreateFailed", errors));
        }

        var rolePermissions = command.Permissions.Select(p => new RolePermission
        {
            RoleId = role.Id,
            FormCode = p.FormCode,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanApprove = p.CanApprove,
            CanCancel = p.CanCancel,
            CanDelete = p.CanDelete
        }).ToList();

        dbContext.RolePermissions.AddRange(rolePermissions);
        await dbContext.SaveChangesAsync(cancellationToken);

        var forms = await dbContext.Forms.Where(f => formCodes.Contains(f.Code)).ToListAsync(cancellationToken);
        return Result.Success(RoleResponseFactory.Build(role, rolePermissions, forms));
    }
}
