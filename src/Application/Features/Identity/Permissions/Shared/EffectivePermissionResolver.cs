namespace ZARI.Application.Features.Identity.Permissions.Shared;

using ZARI.Domain.Common;
using ZARI.Domain.Entities;

// The one place that implements "override replaces, otherwise OR across roles" — reused by the
// GetEffectivePermissions query (drives the FE's button gating) and, later, by server-side
// enforcement in every document handler. Keeping it here instead of duplicating the OR-across-roles
// logic per call site is what makes both places agree on what "effective" means.
public static class EffectivePermissionResolver
{
    public static FormPermissionResponse Resolve(
        Form form,
        IFormPermissionFlags? overrideFlags,
        IEnumerable<IFormPermissionFlags> rolePermissionsForForm)
    {
        if (overrideFlags is not null)
        {
            return new FormPermissionResponse(
                form.Code, form.Name, form.Module,
                overrideFlags.CanView, overrideFlags.CanCreate, overrideFlags.CanEdit,
                overrideFlags.CanApprove, overrideFlags.CanCancel, overrideFlags.CanDelete,
                IsOverridden: true);
        }

        var rolePerms = rolePermissionsForForm as IFormPermissionFlags[] ?? rolePermissionsForForm.ToArray();

        return new FormPermissionResponse(
            form.Code, form.Name, form.Module,
            rolePerms.Any(rp => rp.CanView),
            rolePerms.Any(rp => rp.CanCreate),
            rolePerms.Any(rp => rp.CanEdit),
            rolePerms.Any(rp => rp.CanApprove),
            rolePerms.Any(rp => rp.CanCancel),
            rolePerms.Any(rp => rp.CanDelete),
            IsOverridden: false);
    }
}
