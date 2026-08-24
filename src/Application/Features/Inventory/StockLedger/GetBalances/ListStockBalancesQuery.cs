namespace ZARI.Application.Features.Inventory.StockLedgers.GetBalances;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ListStockBalancesQuery : IQuery<Result<List<StockBalanceResponse>>>;

public sealed record StockBalanceResponse(
    Guid Id,
    Guid ItemId,
    string BranchId,
    Guid WarehouseId,
    string? BatchNo,
    decimal QtyOnHand,
    decimal AvgUnitCost,
    decimal TotalValue,
    DateTimeOffset? LastMovementDate);
