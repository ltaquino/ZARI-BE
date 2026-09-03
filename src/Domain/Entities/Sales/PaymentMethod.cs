namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Admin-configurable catalog of tender types a Customer Payment can be funded by — Cash/Card/Gift
/// Check are just seeded rows here, not a hardcoded enum, since the business explicitly wants to
/// add more payment methods later without a code change. Each row carries its own GL account (the
/// account a tender of this type debits) and which extra fields the POS payment modal must collect
/// for it — a card or gift check needs a reference number and the issuing bank/partner's name; cash
/// needs neither.
/// </summary>
public sealed class PaymentMethod : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public Guid GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = default!;

    public bool RequiresReferenceNo { get; set; }
    // e.g. "Card Number" / "GC Number" — the label the payment modal shows for ReferenceNo when
    // RequiresReferenceNo is set. Null/ignored when RequiresReferenceNo is false.
    public string? ReferenceNoLabel { get; set; }
    public bool RequiresBankOrPartnerName { get; set; }

    public int DisplayOrder { get; set; }
    public string Status { get; set; } = default!;
}
