namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class PurchaseOrder : AuditableEntity
{
    public string PoNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
    public DateTimeOffset OrderDate { get; set; }
    public DateTimeOffset? ExpectedDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
