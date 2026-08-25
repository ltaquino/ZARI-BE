namespace ZARI.Application.Features.Identity.Users.Permissions.GetEffective;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetEffectiveUserPermissionsQueryHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IPermissionService permissionService) : IQueryHandler<GetEffectiveUserPermissionsQuery, Result<List<FormPermissionResponse>>>
{
    public async Task<Result<List<FormPermissionResponse>>> HandleAsync(GetEffectiveUserPermissionsQuery query, CancellationToken cancellationToken = default)
    {
        var isSelf = query.UserId == currentUser.UserId;
        if (!isSelf && !await permissionService.HasPermissionAsync("USERS", FormAction.View, cancellationToken))
            return Result.Failure<List<FormPermissionResponse>>(Error.Forbidden("User.Forbidden", "You do not have permission to view user permissions."));

        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return Result.Failure<List<FormPermissionResponse>>(Error.NotFound("User.NotFound", $"User with ID '{query.UserId}' was not found."));

        var roleNames = await userManager.GetRolesAsync(user);
        var roleIds = await roleManager.Roles.Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync(cancellationToken);

        var forms = await dbContext.Forms.OrderBy(f => f.Module).ThenBy(f => f.Name).ToListAsync(cancellationToken);
        var rolePermissions = await dbContext.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).ToListAsync(cancellationToken);
        var overrides = await dbContext.UserFormPermissionOverrides.Where(o => o.UserId == query.UserId).ToListAsync(cancellationToken);

        var overrideByForm = overrides.ToDictionary(o => o.FormCode, o => (IFormPermissionFlags)o);
        var rolePermsByForm = rolePermissions
            .GroupBy(rp => rp.FormCode)
            .ToDictionary(g => g.Key, g => g.Cast<IFormPermissionFlags>().ToArray());

        var responses = forms
            .Select(form => EffectivePermissionResolver.Resolve(
                form,
                overrideByForm.GetValueOrDefault(form.Code),
                rolePermsByForm.GetValueOrDefault(form.Code, [])))
            .ToList();

        return Result.Success(responses);
    }
}
