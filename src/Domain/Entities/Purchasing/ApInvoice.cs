namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class ApInvoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    // Required — v1 only supports invoices that bill against goods already received via a GRPO.
    public Guid GoodsReceiptPoId { get; set; }
    public GoodsReceiptPo GoodsReceiptPo { get; set; } = default!;

    // The vendor's own real invoice number on the physical bill — the actual reference used to
    // prevent the same bill being entered twice (unique per supplier, see ApInvoiceConfiguration).
    public string SupplierInvoiceNo { get; set; } = default!;

    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<ApInvoiceLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
