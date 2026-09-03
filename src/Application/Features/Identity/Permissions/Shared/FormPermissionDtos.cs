namespace ZARI.Application.Features.Identity.Permissions.Shared;

public sealed record FormPermissionResponse(
    string FormCode,
    string FormName,
    string Module,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanApprove,
    bool CanCancel,
    bool CanDelete,
    // True only when this form has a real per-user override row — false for RoleResponseFactory's
    // own use (a role's own permission set, not a merged user-effective one) and defaults to false
    // there via the trailing default. Drives the Users permission-override screen's own
    // "override this form" toggle so saving doesn't silently freeze every form as an explicit
    // override the moment an admin opens the screen.
    bool IsOverridden = false);

public sealed record FormPermissionInput(
    string FormCode,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanApprove,
    bool CanCancel,
    bool CanDelete);
