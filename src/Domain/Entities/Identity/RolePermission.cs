namespace ZARI.Domain.Entities;

// The reusable "Authorization Group" template (SAP B1 terminology) a Role grants for one Form.
// A user's effective access to a Form is the OR of every role they hold, unless a
// UserFormPermissionOverride exists for that specific (user, form) pair and replaces it outright —
// see EffectivePermissionResolver.
public sealed class RolePermission : ZARI.Domain.Common.IFormPermissionFlags
{
    public string RoleId { get; set; } = default!;
    public string FormCode { get; set; } = default!;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
    public bool CanDelete { get; set; }
}
