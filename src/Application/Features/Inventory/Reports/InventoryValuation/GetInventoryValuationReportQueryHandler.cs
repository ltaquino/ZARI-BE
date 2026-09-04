namespace ZARI.Application.Features.Inventory.Reports.InventoryValuation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Reads today's live StockBalance snapshot (the same denormalized on-hand-value DbSet
/// GetInventoryAsOfQueryHandler's own doc comment describes — unlike that handler, this report
/// wants "right now", not a past point in time, so the live snapshot is exactly right here) joined
/// to Item for CategoryId. The Branch -&gt; Category rollup is a nested group-of-groups, which
/// EF/Pomelo can't translate into one SQL query the same way a flat GroupBy+Sum can — so, following
/// GetInventoryAsOfQueryHandler's own precedent, the candidate rows are pulled once (already
/// filtered by BranchId/CategoryId at the SQL level) and both levels of grouping happen in memory.
/// </summary>
public sealed class GetInventoryValuationReportQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetInventoryValuationReportQuery, Result<InventoryValuationReportResponse>>
{
    public async Task<Result<InventoryValuationReportResponse>> HandleAsync(GetInventoryValuationReportQuery query, CancellationToken cancellationToken = default)
    {
        var balances = await dbContext.StockBalances.AsNoTracking()
            .Include(b => b.Item).ThenInclude(i => i.Category)
            .Where(b => (query.BranchId == null || b.BranchId == query.BranchId)
                && (query.CategoryId == null || b.Item.CategoryId == query.CategoryId))
            .ToListAsync(cancellationToken);

        var branchGroups = balances
            .GroupBy(b => b.BranchId)
            .Select(branchGroup =>
            {
                var categories = branchGroup
                    .GroupBy(b => b.Item.CategoryId)
                    .Select(categoryGroup => new InventoryValuationCategoryRow(
                        categoryGroup.Key,
                        categoryGroup.Key.HasValue ? categoryGroup.First().Item.Category?.Name ?? "Uncategorized" : "Uncategorized",
                        categoryGroup.Sum(b => b.QtyOnHand),
                        Math.Round(categoryGroup.Sum(b => b.TotalValue), 2)))
                    .OrderByDescending(c => c.TotalValue)
                    .ToList();

                return new InventoryValuationBranchGroup(
                    branchGroup.Key,
                    Math.Round(categories.Sum(c => c.TotalValue), 2),
                    categories);
            })
            .OrderByDescending(b => b.BranchTotalValue)
            .ToList();

        var response = new InventoryValuationReportResponse(
            branchGroups,
            Math.Round(branchGroups.Sum(b => b.BranchTotalValue), 2),
            branchGroups.SelectMany(b => b.Categories).Sum(c => c.QtyOnHand));

        return Result.Success(response);
    }
}
