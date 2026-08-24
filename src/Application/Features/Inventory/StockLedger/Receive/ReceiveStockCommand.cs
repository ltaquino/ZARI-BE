namespace ZARI.Application.Features.Inventory.StockLedgers.Receive;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ReceiveStockCommand(
    Guid ItemId,
    string BranchId,
    Guid WarehouseId,
    string? BatchNo,
    decimal Qty,
    decimal UnitCost,
    string ReferenceTable,
    string ReferenceId,
    DateTimeOffset TransactionDate,
    string? TransactionType) : ICommand<Result<ReceiveStockResponse>>;

/// UnitCost is null when the item isn't stocked (e.g. a Service item) — a documented no-op, not an error.
public sealed record ReceiveStockResponse(decimal? UnitCost);
