namespace ZARI.Domain.Entities;

// A user's branch assignments — orthogonal to Form permissions. Which branches a user may act
// within is checked separately from what actions they may take (RolePermission /
// UserFormPermissionOverride).
public sealed class UserBranch
{
    public string UserId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
}
