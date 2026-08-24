namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Generic, polymorphic on (EntityType, EntityId) — reusable by any document type without schema
/// changes here. Mirrors the FE prototype's data/workflow/approvalRequests.ts. RequestType SUBMIT
/// covers a normal submit-for-approval; CANCEL covers requesting to reverse an already-posted
/// document (see e.g. GoodsReceipt's PENDING_CANCELLATION state).
/// </summary>
public sealed class ApprovalRequest : AuditableEntity
{
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    // References the Users mock/system-module data — same rationale, plain string not FK.
    public string RequestedBy { get; set; } = default!;

    public DateTimeOffset RequestedAt { get; set; }
    public string Status { get; set; } = default!;
    public string RequestType { get; set; } = default!;
    public string? Reason { get; set; }

    public List<ApprovalAction> Actions { get; set; } = [];
}
