namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Generic, polymorphic on (EntityType, EntityId) — same pattern as ApprovalRequest. Who may
/// actually SEE a given notification is an entity-specific decision made by the FE consumer
/// (see e.g. ZARI-FE/src/features/inventory/goodsReceipts/notifications.ts) — this entity itself
/// carries no authorization logic, matching the FE prototype.
/// </summary>
public sealed class Notification : AuditableEntity
{
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? ActorUserId { get; set; }

    public List<NotificationRead> Reads { get; set; } = [];
}
