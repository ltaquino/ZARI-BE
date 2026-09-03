namespace ZARI.Application.Features.Identity.Roles.GetAll;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllRolesQueryHandler(
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : IQueryHandler<GetAllRolesQuery, Result<List<RoleResponse>>>
{
    public async Task<Result<List<RoleResponse>>> HandleAsync(GetAllRolesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ROLES", FormAction.View, cancellationToken))
            return Result.Failure<List<RoleResponse>>(Error.Forbidden("Role.Forbidden", "You do not have permission to view roles."));

        var roles = await roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var allPermissions = await dbContext.RolePermissions.AsNoTracking().ToListAsync(cancellationToken);
        var forms = await dbContext.Forms.AsNoTracking().ToListAsync(cancellationToken);

        var responses = roles
            .Select(role => RoleResponseFactory.Build(role, allPermissions.Where(rp => rp.RoleId == role.Id).ToList(), forms))
            .ToList();

        return Result.Success(responses);
    }
}
