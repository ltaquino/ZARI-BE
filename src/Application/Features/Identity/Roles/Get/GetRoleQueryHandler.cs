namespace ZARI.Application.Features.Identity.Roles.Get;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;

public sealed class GetRoleQueryHandler(
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : IQueryHandler<GetRoleQuery, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> HandleAsync(GetRoleQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.View, cancellationToken))
            return Result.Failure<RoleResponse>(Error.Forbidden("Role.Forbidden", "You do not have permission to view roles."));

        var role = await roleManager.FindByIdAsync(query.Id);
        if (role is null)
            return Result.Failure<RoleResponse>(Error.NotFound("Role.NotFound", $"Role with ID '{query.Id}' was not found."));

        var rolePermissions = await dbContext.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync(cancellationToken);
        var forms = await dbContext.Forms.ToListAsync(cancellationToken);

        return Result.Success(RoleResponseFactory.Build(role, rolePermissions, forms));
    }
}
