namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class CustomerPaymentLine : BaseEntity
{
    public Guid CustomerPaymentId { get; set; }
    public CustomerPayment CustomerPayment { get; set; } = default!;

    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = default!;

    // How much of this payment is applied to the referenced invoice — may be a partial amount,
    // re-checked against the invoice's own remaining balance at Approve time.
    public decimal AmountApplied { get; set; }
}
