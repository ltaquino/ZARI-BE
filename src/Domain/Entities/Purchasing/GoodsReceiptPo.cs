namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReceiptPo : AuditableEntity
{
    public string GrpoNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    // Optional — creating a GRPO doesn't require a PO on file (e.g. a walk-in delivery).
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    // The vendor's own DR/invoice number on the physical delivery — tracking only, not a system doc.
    public string? SupplierInvoiceNo { get; set; }

    public DateTimeOffset ReceiptDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<GoodsReceiptPoLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
