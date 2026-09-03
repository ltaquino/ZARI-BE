namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Customer : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string Status { get; set; } = default!;

    // The salesperson/account owner's display name — plain string, not a User FK, since Users
    // aren't a backend entity yet either.
    public string Owner { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Notes { get; set; }

    // Free-text cooperative member number — searchable from POS Mode alongside Name. No format
    // enforced; the business's existing member-card numbering (whatever it is) is entered as-is.
    public string? MemberNo { get; set; }

    // Mirrors Supplier.ApAccountId/PaymentTermsDays (Phase 17) exactly, on the AR side: an override
    // GL account for this customer's receivable (falls back to "1200" Accounts Receivable when
    // null) and a net-days default for Sales Invoice due dates (null = no default, purely manual).
    public Guid? ArAccountId { get; set; }
    public GlAccount? ArAccount { get; set; }
    public int? PaymentTermsDays { get; set; }

    // Discount Scheme (ZARI-FE/frs/sales/DiscountSchemeContext.md §2.2): a standing % suggested by
    // default on every new Sales Order/Invoice line for this customer — a suggestion only, always
    // freely overridable, never enforced.
    public decimal? StandingDiscountPct { get; set; }
}
