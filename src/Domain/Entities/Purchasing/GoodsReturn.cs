namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReturn : AuditableEntity
{
    public string ReturnNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    // Optional — a return doesn't have to trace back to a specific GRPO on file.
    public Guid? GoodsReceiptPoId { get; set; }
    public GoodsReceiptPo? GoodsReceiptPo { get; set; }

    // Loose string reference to PurchaseReturnReason.Code — validated in the handler, not an EF FK.
    public string ReasonCode { get; set; } = default!;

    public DateTimeOffset ReturnDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<GoodsReturnLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
