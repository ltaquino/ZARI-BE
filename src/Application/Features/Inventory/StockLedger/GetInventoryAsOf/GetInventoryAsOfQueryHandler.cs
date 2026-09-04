namespace ZARI.Application.Features.Inventory.StockLedgers.GetInventoryAsOf;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Reconstructs true point-in-time ending balances from StockLedger history rather than reading
/// today's live StockBalance snapshot — correct even when run weeks after a past fiscal year-end.
/// Filters candidate rows by TransactionDate (business date), then within that filtered set takes
/// each (Item, Warehouse, Batch) group's *last* row ordered by PostedAt — the same append-order
/// field ListStockLedgerEntriesQueryHandler already uses as this ledger's own true sequence — whose
/// RunningBalanceQty/RunningBalanceValue already *is* the correct as-of balance, no re-summing
/// needed. Grouping happens in memory (not translated to SQL): MySQL/Pomelo can't express a
/// "latest row per group" query the way SQL Server's APPLY can, and this is a bounded, occasional-use
/// report (an annual filing), not a hot path — pulling every ledger row up to the cutoff date once
/// is an acceptable cost here.
/// </summary>
public sealed class GetInventoryAsOfQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetInventoryAsOfQuery, Result<List<InventoryAsOfLineResponse>>>
{
    public async Task<Result<List<InventoryAsOfLineResponse>>> HandleAsync(GetInventoryAsOfQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.StockLedgers
            .Where(l => l.TransactionDate <= query.AsOfDate && (query.BranchId == null || l.BranchId == query.BranchId))
            .OrderBy(l => l.PostedAt)
            .ToListAsync(cancellationToken);

        var warehouseNames = await dbContext.Warehouses.ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        // Negative-balance rows are still included here — same convention TrialBalanceReportPage
        // uses (return everything, let the caller's own "show zero balances" toggle decide what's
        // hidden). Zero-balance rows are dropped unless IncludeZero is set — this used to be a
        // client-side post-filter (rows.filter(r => r.qtyOnHand !== 0)), now applied server-side.
        var response = candidates
            .GroupBy(l => (l.ItemId, l.WarehouseId, l.BatchNo))
            .Select(g => g.Last())
            .Select(l => new InventoryAsOfLineResponse(
                l.ItemId, l.ItemCode, l.ItemName, l.UomCode,
                l.BranchId, l.WarehouseId, warehouseNames.GetValueOrDefault(l.WarehouseId, "Unknown"), l.BatchNo,
                l.RunningBalanceQty,
                l.RunningBalanceQty != 0 ? Math.Round(l.RunningBalanceValue / l.RunningBalanceQty, 4) : 0,
                l.RunningBalanceValue))
            .Where(l => query.IncludeZero || l.QtyOnHand != 0)
            .OrderBy(l => l.ItemCode)
            .ToList();

        return Result.Success(response);
    }
}
