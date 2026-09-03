namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One split-tender line on a Customer Payment — the FUNDING side (how the payment was made),
/// distinct from CustomerPaymentLine (the ALLOCATION side — which invoice(s) it covers). A payment
/// with any Tenders posts its GL debit per-tender against each PaymentMethod's own GL account
/// instead of the payment header's single legacy CashAccountId — see
/// CustomerPaymentPostingService. Optional: a payment with zero tenders falls back to that legacy
/// single-account behavior exactly as Wave 4 originally built it.
/// </summary>
public sealed class CustomerPaymentTender : BaseEntity
{
    public Guid CustomerPaymentId { get; set; }
    public CustomerPayment CustomerPayment { get; set; } = default!;

    public Guid PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = default!;

    public decimal Amount { get; set; }

    // The card/GC/etc. number — required only when PaymentMethod.RequiresReferenceNo is set.
    public string? ReferenceNo { get; set; }
    // The issuing bank / GC partner's name — required only when PaymentMethod.RequiresBankOrPartnerName is set.
    public string? BankOrPartnerName { get; set; }
}
