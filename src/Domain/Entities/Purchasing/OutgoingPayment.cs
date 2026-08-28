namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Closes the procure-to-pay cycle: pays off one or more posted (unpaid) AP Invoices for a single
/// supplier out of a single bank/cash account, converting the "2000" Accounts Payable liability
/// into an actual cash/bank outflow. Each line pays its referenced invoice in full — v1 has no
/// partial-payment or price-variance handling, same simplicity choice as AP Invoice itself.
/// </summary>
public sealed class OutgoingPayment : AuditableEntity
{
    public string PaymentNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = default!;

    public DateTimeOffset PaymentDate { get; set; }

    // The check number / bank transfer reference from the physical payment instrument — tracking
    // only, same free-text role as GoodsReceiptPo.SupplierInvoiceNo.
    public string? RefNo { get; set; }

    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<OutgoingPaymentLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
