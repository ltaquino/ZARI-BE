namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// The system's actual BIR-facing receipt. Books AR/Revenue/VAT Payable on Approve (no stock
/// effect — Delivery already moved stock/COGS). Carries a second, BIR-series receipt number
/// (BirOrSeriesNumber) alongside its own internal InvoiceNo — see SalesModuleContext.md §3.7.
/// </summary>
public sealed class SalesInvoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    // Optional — an Invoice doesn't have to trace back to a Delivery on file (e.g. a service sale
    // with nothing to physically ship).
    public Guid? DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }

    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public decimal? DiscountPct { get; set; }

    // The BIR-compliant "OR/SI No." actually printed on the receipt — assigned at Approve/
    // quick-post time from this branch's own "BIR-OR" DocumentSequence series. Null until posted.
    public string? BirOrSeriesNumber { get; set; }

    // Phase-12-style partial-payment tracking, mirrored from ApInvoice: PaidAmount accumulates as
    // CustomerPayment allocations are approved against this invoice; Status flips to
    // PARTIALLY_PAID/PAID independently of the document workflow Status above.
    public decimal PaidAmount { get; set; }

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public List<SalesInvoiceLine> Lines { get; set; } = [];

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
