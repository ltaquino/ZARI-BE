namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Unified master data covering three of the Discount Scheme's six mechanisms in one shape (see
/// ZARI-FE/frs/sales/DiscountSchemeContext.md §2.3): item/category standard discounts, quantity
/// tiers (via MinQty), and time-boxed promotional campaigns (via StartDate/EndDate) — the only
/// difference between them is which optional fields are populated.
///
/// Also doubles as the SKU allow-list a discretionary line discount is checked against (Phase 46 —
/// see Application.Features.Sales.Shared.DiscountEligibility): a line's manual DiscountPct is only
/// permitted once at least one ACTIVE row here covers its item/category, branch, quantity, and
/// (if date-boxed) today's promo window. This only gates eligibility — it never caps the % actually
/// entered at this row's own DiscountValue.
/// </summary>
public sealed class DiscountRule : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    /// "ITEM" | "CATEGORY" | "ALL"
    public string Scope { get; set; } = default!;

    // Populated only for Scope="ITEM" — one or more specific SKUs (Phase 46+: a promo spanning
    // several items is one rule with several of these, not N duplicate rules).
    public List<DiscountRuleItem> Items { get; set; } = [];

    public Guid? ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }

    /// "PERCENT" | "FIXED_AMOUNT"
    public string DiscountType { get; set; } = default!;
    public decimal DiscountValue { get; set; }

    // Null = applies at any qty. Set = this rule only kicks in at/above this qty — several rows on
    // the same Item/Category form a tier ladder (e.g. MinQty=10 -> 5%, MinQty=50 -> 10%).
    public decimal? MinQty { get; set; }

    // Both null = a standing, non-promotional rule. Both set = a promotional campaign — matched
    // against the document's own date, not "today" at data-entry time.
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // Null = all branches.
    public string? BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Tie-break order when multiple rules could match the same line.
    public int Priority { get; set; }

    public string Status { get; set; } = default!;
}
