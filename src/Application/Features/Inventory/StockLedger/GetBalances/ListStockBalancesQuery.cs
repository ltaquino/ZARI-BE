namespace ZARI.Application.Features.Inventory.StockLedgers.GetBalances;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <param name="BranchId">Only this branch's balances (a device only needs its own branches').</param>
// Delta sync (ZARI Mobile): only rows created or changed at/after this instant. Deletions aren't reported — a client
// still does an occasional full refresh to prune.
public sealed record ListStockBalancesQuery(string? BranchId = null, DateTimeOffset? UpdatedSince = null) : IQuery<Result<List<StockBalanceResponse>>>;

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
