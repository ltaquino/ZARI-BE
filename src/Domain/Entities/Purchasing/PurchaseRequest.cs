namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class PurchaseRequest : AuditableEntity
{
    public string RequestNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public DateTimeOffset RequestDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<PurchaseRequestLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
