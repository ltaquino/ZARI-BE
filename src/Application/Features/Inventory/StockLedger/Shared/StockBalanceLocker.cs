namespace ZARI.Application.Features.Inventory.StockLedgers.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Locks every existing StockBalance row for each distinct (ItemId, WarehouseId) pair — not
/// filtered by batch, since a blank-batch issue line can draw from every batch bucket for that
/// item/warehouse (see the StockBalance/StockReservation type comments on the FE side this mirrors).
/// Under MySQL/InnoDB's default REPEATABLE READ, a "SELECT ... WHERE ItemId = ? AND WarehouseId = ?
/// FOR UPDATE" against the (ItemId, WarehouseId, BatchNo) index also gap-locks that whole range, so
/// a concurrent transaction can't insert a brand-new batch bucket for the same item/warehouse while
/// we hold this lock either — that's what makes "check availability, then act" safe here without a
/// separate insert-race workaround.
///
/// This is the ONE lock that protects all derived state for an (Item, Warehouse) pair — balances
/// AND cost layers. Every handler that reads or mutates CostLayers for a key must acquire this lock
/// first; nothing separately locks CostLayers rows.
/// </summary>
internal static class StockBalanceLocker
{
    public static async Task<List<StockBalance>> LockItemWarehousePairsAsync(
        IAppDbContext dbContext,
        IEnumerable<(Guid ItemId, Guid WarehouseId)> itemWarehousePairs,
        CancellationToken cancellationToken)
    {
        var distinctPairs = itemWarehousePairs
            .Distinct()
            .OrderBy(p => p.ItemId)
            .ThenBy(p => p.WarehouseId)
            .ToList();

        var result = new List<StockBalance>();
        foreach (var (itemId, warehouseId) in distinctPairs)
        {
            var rows = await dbContext.StockBalances
                .FromSqlInterpolated($"SELECT * FROM StockBalances WHERE ItemId = {itemId} AND WarehouseId = {warehouseId} FOR UPDATE")
                .ToListAsync(cancellationToken);
            result.AddRange(rows);
        }

        return result;
    }

    public static string? NormalizeBatch(string? batchNo)
    {
        var trimmed = batchNo?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static bool SameBatch(string? a, string? b) => NormalizeBatch(a) == NormalizeBatch(b);

    /// <summary>
    /// Finds the locked balance row for the exact (itemId, warehouseId, batchNo) key — batchNo is
    /// matched as-is (including null), unlike CandidateBalances where a null batchNo means "every
    /// batch is a candidate".
    /// </summary>
    public static StockBalance? FindExact(List<StockBalance> lockedRows, Guid itemId, Guid warehouseId, string? batchNo)
    {
        var normalized = NormalizeBatch(batchNo);
        return lockedRows.FirstOrDefault(b => b.ItemId == itemId && b.WarehouseId == warehouseId && SameBatch(b.BatchNo, normalized));
    }

    /// <summary>
    /// Same lookup as FindExact, but creates and tracks a new zeroed row if this is the first
    /// movement ever for that exact batch. Safe to insert here without a further existence check:
    /// the caller already holds the (ItemId, WarehouseId) gap lock via LockItemWarehousePairsAsync.
    /// </summary>
    public static StockBalance GetOrCreate(
        IAppDbContext dbContext,
        List<StockBalance> lockedRows,
        Guid itemId,
        string branchId,
        Guid warehouseId,
        string? batchNo)
    {
        var existing = FindExact(lockedRows, itemId, warehouseId, batchNo);
        if (existing is not null) return existing;

        var normalized = NormalizeBatch(batchNo);

        var created = new StockBalance
        {
            ItemId = itemId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            BatchNo = normalized,
            QtyOnHand = 0,
            AvgUnitCost = 0,
            TotalValue = 0
        };
        dbContext.StockBalances.Add(created);
        lockedRows.Add(created);
        return created;
    }

    /// <summary>
    /// Balance buckets available to draw from/restore for (itemId, warehouseId, batchNo) within an
    /// already-locked set. An exact batch is honored as-is; a null batch means every batch bucket
    /// for that item/warehouse is a candidate — a batch-tracked item shouldn't read as "no stock"
    /// just because its stock happens to live under a named lot the caller didn't ask for.
    /// Oldest-moved bucket first, as a FIFO/FEFO-ish order.
    /// </summary>
    public static List<StockBalance> CandidateBalances(List<StockBalance> lockedRows, Guid itemId, Guid warehouseId, string? batchNo, bool onlyPositive = true)
    {
        return lockedRows
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId
                && (batchNo is null || SameBatch(b.BatchNo, batchNo))
                && (!onlyPositive || b.QtyOnHand > 0.0001m))
            .OrderBy(b => b.LastMovementDate ?? DateTimeOffset.MinValue)
            .ToList();
    }
}
