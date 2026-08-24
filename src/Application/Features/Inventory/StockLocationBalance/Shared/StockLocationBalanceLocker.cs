namespace ZARI.Application.Features.Inventory.StockLocationBalances.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Locks every existing StockLocationBalance row for a given (ItemId, WarehouseId) pair — mirrors
/// StockBalanceLocker's gap-locking rationale, scoped one level finer (per bin instead of per
/// batch bucket). A bin move touches two locations at once, both under the same (ItemId,
/// WarehouseId) lock, so no separate deadlock-avoidance ordering is needed beyond the single
/// FOR UPDATE scan already sorted by the caller.
/// </summary>
internal static class StockLocationBalanceLocker
{
    public static async Task<List<StockLocationBalance>> LockItemWarehouseAsync(
        IAppDbContext dbContext,
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StockLocationBalances
            .FromSqlInterpolated($"SELECT * FROM StockLocationBalances WHERE ItemId = {itemId} AND WarehouseId = {warehouseId} FOR UPDATE")
            .ToListAsync(cancellationToken);
    }

    public static string? NormalizeBatch(string? batchNo)
    {
        var trimmed = batchNo?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static bool SameBatch(string? a, string? b) => NormalizeBatch(a) == NormalizeBatch(b);

    public static StockLocationBalance? FindExact(List<StockLocationBalance> lockedRows, Guid itemId, Guid warehouseId, Guid locationId, string? batchNo)
    {
        var normalized = NormalizeBatch(batchNo);
        return lockedRows.FirstOrDefault(b =>
            b.ItemId == itemId && b.WarehouseId == warehouseId && b.LocationId == locationId && SameBatch(b.BatchNo, normalized));
    }

    public static StockLocationBalance GetOrCreate(
        IAppDbContext dbContext,
        List<StockLocationBalance> lockedRows,
        Guid itemId,
        Guid warehouseId,
        Guid locationId,
        string? batchNo)
    {
        var existing = FindExact(lockedRows, itemId, warehouseId, locationId, batchNo);
        if (existing is not null) return existing;

        var created = new StockLocationBalance
        {
            ItemId = itemId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            BatchNo = NormalizeBatch(batchNo),
            QtyOnHand = 0
        };
        dbContext.StockLocationBalances.Add(created);
        lockedRows.Add(created);
        return created;
    }
}
