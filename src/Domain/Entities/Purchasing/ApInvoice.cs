namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class ApInvoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    // "ITEM" (default, v1's original scope) bills against a posted GRPO's received lines.
    // "EXPENSE" bills a vendor directly with no GRPO at all — utilities, professional fees,
    // manpower/salaries, and other costs that never go through receiving. Immutable once created,
    // same as GoodsReceiptPoId below — mirrors which of Lines/ExpenseLines is populated.
    public string InvoiceType { get; set; } = default!;

    // Required for ITEM invoices, always null for EXPENSE invoices.
    public Guid? GoodsReceiptPoId { get; set; }
    public GoodsReceiptPo? GoodsReceiptPo { get; set; }

    // The vendor's own real invoice number on the physical bill — the actual reference used to
    // prevent the same bill being entered twice (unique per supplier, see ApInvoiceConfiguration).
    public string SupplierInvoiceNo { get; set; } = default!;

    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    // Populated for ITEM invoices; empty for EXPENSE invoices.
    public List<ApInvoiceLine> Lines { get; set; } = [];

    // Populated for EXPENSE invoices; empty for ITEM invoices.
    public List<ApInvoiceExpenseLine> ExpenseLines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
