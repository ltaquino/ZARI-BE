namespace ZARI.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class PermissionService(
    ICurrentUser currentUser,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(string formCode, FormAction action, CancellationToken cancellationToken = default)
    {
        var effective = await ResolveAsync(formCode, cancellationToken);
        return effective is not null && Check(effective, action);
    }

    public async Task<bool> HasPermissionOnBranchAsync(string formCode, FormAction action, string branchId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return false;

        var isAssignedToBranch = await dbContext.UserBranches
            .AnyAsync(ub => ub.UserId == currentUser.UserId && ub.BranchId == branchId, cancellationToken);
        if (!isAssignedToBranch) return false;

        return await HasPermissionAsync(formCode, action, cancellationToken);
    }

    public async Task<bool> HasCancellationAuthorityAsync(string formCode, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return false;

        var isAssignedToHq = await dbContext.UserBranches
            .Join(dbContext.Branches, ub => ub.BranchId, b => b.Id, (ub, b) => new { ub.UserId, b.IsHeadOffice })
            .AnyAsync(x => x.UserId == currentUser.UserId && x.IsHeadOffice, cancellationToken);
        if (!isAssignedToHq) return false;

        return await HasPermissionAsync(formCode, FormAction.Cancel, cancellationToken);
    }

    private async Task<FormPermissionResponse?> ResolveAsync(string formCode, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return null;

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user is null) return null;

        var form = await dbContext.Forms.FirstOrDefaultAsync(f => f.Code == formCode, cancellationToken);
        if (form is null) return null;

        var roleNames = await userManager.GetRolesAsync(user);
        var roleIds = await roleManager.Roles.Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync(cancellationToken);

        var rolePermissions = await dbContext.RolePermissions
            .Where(rp => rp.FormCode == formCode && roleIds.Contains(rp.RoleId))
            .ToListAsync(cancellationToken);

        var overrideRow = await dbContext.UserFormPermissionOverrides
            .FirstOrDefaultAsync(o => o.UserId == currentUser.UserId && o.FormCode == formCode, cancellationToken);

        return EffectivePermissionResolver.Resolve(form, overrideRow, rolePermissions);
    }

    private static bool Check(FormPermissionResponse permission, FormAction action) => action switch
    {
        FormAction.View => permission.CanView,
        FormAction.Create => permission.CanCreate,
        FormAction.Edit => permission.CanEdit,
        FormAction.Approve => permission.CanApprove,
        FormAction.Cancel => permission.CanCancel,
        FormAction.Delete => permission.CanDelete,
        _ => false
    };
}
