namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class OutgoingPaymentLine : BaseEntity
{
    public Guid OutgoingPaymentId { get; set; }
    public OutgoingPayment OutgoingPayment { get; set; } = default!;

    public Guid ApInvoiceId { get; set; }
    public ApInvoice ApInvoice { get; set; } = default!;

    // Snapshot of the invoice's total at the time it was added to this payment — always equal to
    // the invoice's own line total in v1 (full payment only), re-checked at Approve time.
    public decimal Amount { get; set; }
}
