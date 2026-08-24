namespace ZARI.Application.Features.Inventory.StockLedgers.GetLedgerEntries;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ListStockLedgerEntriesQuery(Guid ItemId, Guid WarehouseId, string? BatchNo) : IQuery<Result<List<StockLedgerEntryResponse>>>;

public sealed record StockLedgerConsumptionResponse(Guid LayerId, decimal Qty);

public sealed record StockLedgerBalanceDrawResponse(string? BatchNo, decimal Qty, decimal UnitCost);

public sealed record StockLedgerEntryResponse(
    Guid Id,
    Guid ItemId,
    string? ItemCode,
    string? ItemName,
    string? UomCode,
    string BranchId,
    Guid WarehouseId,
    string? BatchNo,
    string TransactionType,
    string ReferenceTable,
    string ReferenceId,
    decimal QtyIn,
    decimal QtyOut,
    decimal UnitCost,
    decimal RunningBalanceQty,
    decimal RunningBalanceValue,
    bool IsReversal,
    List<StockLedgerConsumptionResponse>? Consumptions,
    List<StockLedgerBalanceDrawResponse>? BalanceDraws,
    DateTimeOffset TransactionDate,
    DateTimeOffset PostedAt);
