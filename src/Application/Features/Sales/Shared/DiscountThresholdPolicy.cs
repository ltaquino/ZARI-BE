namespace ZARI.Application.Features.Sales.Shared;

/// <summary>
/// The hybrid discount-approval mechanic (DiscountSchemeContext.md §2.6 + this session's build
/// decision): a document's discretionary discount is checked against
/// <c>Company.MaxUnapprovedDiscountPct</c> to decide whether it must go through the normal
/// DRAFT -&gt; PENDING_APPROVAL -&gt; POSTED workflow, or may be quick-posted straight from Create
/// when its document type's own quick-post toggle is on. Statutory discounts (see
/// StatutoryDiscountType) are a legal entitlement, not a staff-granted concession — never pass
/// them into <paramref name="lineDiscountPcts"/>.
/// </summary>
internal static class DiscountThresholdPolicy
{
    public static bool ExceedsThreshold(decimal? maxUnapprovedDiscountPct, decimal? headerDiscountPct, IEnumerable<decimal> lineDiscountPcts)
    {
        if (!maxUnapprovedDiscountPct.HasValue)
            return false;

        var maxDiscount = lineDiscountPcts.DefaultIfEmpty(0).Max();
        if (headerDiscountPct.HasValue)
            maxDiscount = Math.Max(maxDiscount, headerDiscountPct.Value);

        return maxDiscount > maxUnapprovedDiscountPct.Value;
    }
}
