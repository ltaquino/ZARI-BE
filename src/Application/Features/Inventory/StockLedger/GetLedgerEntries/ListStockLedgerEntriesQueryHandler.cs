namespace ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class ListStockLedgerEntriesQueryHandler(IAppDbContext dbContext) : IQueryHandler<ListStockLedgerEntriesQuery, Result<List<StockLedgerEntryResponse>>>
{
    public async Task<Result<List<StockLedgerEntryResponse>>> HandleAsync(ListStockLedgerEntriesQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedBatch = string.IsNullOrWhiteSpace(query.BatchNo) ? null : query.BatchNo.Trim();

        var rows = await dbContext.StockLedgers.AsNoTracking()
            .Where(l => l.ItemId == query.ItemId && l.WarehouseId == query.WarehouseId
                && (normalizedBatch == null ? l.BatchNo == null : l.BatchNo == normalizedBatch))
            .OrderBy(l => l.PostedAt)
            .ToListAsync(cancellationToken);

        var response = rows.Select(l => new StockLedgerEntryResponse(
            l.Id, l.ItemId, l.ItemCode, l.ItemName, l.UomCode,
            l.BranchId, l.WarehouseId, l.BatchNo,
            l.TransactionType, l.ReferenceTable, l.ReferenceId,
            l.QtyIn, l.QtyOut, l.UnitCost,
            l.RunningBalanceQty, l.RunningBalanceValue, l.IsReversal,
            Deserialize<List<StockLedgerConsumptionResponse>>(l.ConsumptionsJson),
            Deserialize<List<StockLedgerBalanceDrawResponse>>(l.BalanceDrawsJson),
            l.TransactionDate, l.PostedAt))
            .ToList();

        return Result.Success(response);
    }

    private static T? Deserialize<T>(string? json) where T : class
        => json is null ? null : JsonSerializer.Deserialize<T>(json);
}
