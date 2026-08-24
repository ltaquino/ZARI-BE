namespace ZARI.Application.Features.Inventory.StockLocationBalances.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockLocationBalancesQuery : IQuery<Result<List<StockLocationBalanceResponse>>>;

public sealed record StockLocationBalanceResponse(
    Guid Id,
    Guid ItemId,
    Guid WarehouseId,
    Guid LocationId,
    string? BatchNo,
    decimal QtyOnHand,
    DateTimeOffset? LastMovementDate);
