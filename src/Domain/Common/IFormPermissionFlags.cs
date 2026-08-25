namespace ZARI.Domain.Common;

// Shared shape of RolePermission and UserFormPermissionOverride — lets EffectivePermissionResolver
// combine either kind of row without duplicating the six-flag list per call site.
public interface IFormPermissionFlags
{
    bool CanView { get; }
    bool CanCreate { get; }
    bool CanEdit { get; }
    bool CanApprove { get; }
    bool CanCancel { get; }
    bool CanDelete { get; }
}
