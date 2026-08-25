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
    bool CanDelete);

public sealed record FormPermissionInput(
    string FormCode,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanApprove,
    bool CanCancel,
    bool CanDelete);
