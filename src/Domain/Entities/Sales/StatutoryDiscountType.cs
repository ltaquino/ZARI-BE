namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Catalog of Philippine statutory/special-law discounts (Senior Citizen RA 9994, PWD RA 10754,
/// National Athletes/Coaches RA 10699, Solo Parents RA 11861) — each a legally fixed % that also
/// carries a possible VAT-exemption side effect and requires the buyer's qualifying ID captured on
/// the invoice line. See ZARI-FE/frs/sales/DiscountSchemeContext.md §4.6 — seeded rates are a
/// starting checklist, not a legal citation; verify against each law's current IRR before go-live.
/// </summary>
public sealed class StatutoryDiscountType : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal DiscountPct { get; set; }
    public bool IsVatExempt { get; set; }

    // e.g. "Senior Citizen ID No.", "PWD ID No." — shown as the label for SalesInvoiceLine.
    // StatutoryIdNumber's input when this type is selected.
    public string RequiredIdLabel { get; set; } = default!;

    public string Status { get; set; } = default!;
}
