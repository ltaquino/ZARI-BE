namespace ZARI.Domain.Entities;

// Presence of a row here for a (user, form) pair fully replaces whatever that user's roles would
// grant for that form — not additive — matching how a B1 admin picks a different level for one
// user on one form. See EffectivePermissionResolver for the resolution rule.
public sealed class UserFormPermissionOverride : ZARI.Domain.Common.IFormPermissionFlags
{
    public string UserId { get; set; } = default!;
    public string FormCode { get; set; } = default!;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
    public bool CanDelete { get; set; }
}
