namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReceipt : AuditableEntity
{
    public string GrNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public string ReceiptType { get; set; } = default!;
    public string? ReceivedBy { get; set; }
    public DateTimeOffset GrDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = [];

    // Goods Issue references are only meaningful for TRANSFER_IN receipts. GoodsIssue isn't a
    // backend entity yet, so this stays a plain string (not Guid/FK) until it exists.
    public string? GoodsIssueRefNo { get; set; }
    public string? GoodsIssueId { get; set; }

    // AdjustmentReason code — required for MANUAL receipts, used to resolve the GL variance offset.
    public string? ReasonCode { get; set; }

    // Optional departmental tag, applied to every line of this receipt's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
