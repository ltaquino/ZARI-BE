namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Closes the order-to-cash cycle: applies cash/bank receipts against one or more posted Sales
/// Invoices for a single customer, converting "1200" Accounts Receivable into an actual cash/bank
/// inflow. Mirrors OutgoingPayment, on the AR side, with Phase-12-style partial allocation (an
/// invoice doesn't have to be paid off in full by a single payment).
/// </summary>
public sealed class CustomerPayment : AuditableEntity
{
    public string PaymentNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public string PaymentMethod { get; set; } = default!;

    // The GL account the cash/bank inflow is debited to (e.g. "1000" Cash on Hand, or a real bank
    // account chosen at entry) — free-standing, not restricted to BankAccount like Purchasing's
    // OutgoingPayment, since retail receipts are commonly straight cash.
    public Guid CashAccountId { get; set; }
    public GlAccount CashAccount { get; set; } = default!;

    public DateTimeOffset PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<CustomerPaymentLine> Lines { get; set; } = [];

    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
