namespace ZARI.Application.Features.Sales.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// The hard-block gate behind "a discretionary discount can only be applied to specific SKUs" — a
/// line's manual DiscountPct is only allowed once at least one ACTIVE DiscountRule currently covers
/// it: by its own ItemId, by its ItemCategoryId, or a store-wide "ALL" rule — scoped to this branch
/// (or a branch-agnostic rule), this line's own quantity against any MinQty tier, and today's promo
/// window if the rule is date-boxed.
///
/// This only answers eligibility (yes/no) — it does NOT cap the discount at the rule's own
/// DiscountValue; an eligible item still takes whatever % the encoder types (the "hard block, not a
/// value cap" design decision). Statutory discounts (StatutoryDiscountTypeId) are a completely
/// separate legal-entitlement mechanism and never pass through this gate — callers must exclude
/// those lines before checking.
///
/// Line-level only: the document's header DiscountPct (one blanket % across the whole cart, never
/// SKU-specific by nature) deliberately stays outside this gate.
/// </summary>
internal static class DiscountEligibility
{
    /// <summary>One query for the whole document — every ACTIVE rule that could possibly apply at
    /// this branch (its own branch-specific rules, plus every branch-agnostic rule), with its
    /// covered items eagerly loaded. The per-line scope/qty/date match then happens in memory via
    /// <see cref="IsEligible"/>, so N lines cost one round trip, not N.</summary>
    public static async Task<List<DiscountRule>> GetActiveRulesAsync(IAppDbContext dbContext, string branchId, CancellationToken cancellationToken)
    {
        return await dbContext.DiscountRules
            .Include(r => r.Items)
            .Where(r => r.Status == "active" && (r.BranchId == null || r.BranchId == branchId))
            .ToListAsync(cancellationToken);
    }

    public static bool IsEligible(IReadOnlyCollection<DiscountRule> activeRules, Guid itemId, Guid? categoryId, DateOnly asOfDate, decimal qty)
    {
        return activeRules.Any(r =>
            (r.Scope == "ALL" || (r.Scope == "ITEM" && r.Items.Any(i => i.ItemId == itemId)) || (r.Scope == "CATEGORY" && categoryId is not null && r.ItemCategoryId == categoryId))
            && (r.MinQty is null || qty >= r.MinQty.Value)
            && (r.StartDate is null || r.StartDate.Value <= asOfDate)
            && (r.EndDate is null || r.EndDate.Value >= asOfDate));
    }
}
