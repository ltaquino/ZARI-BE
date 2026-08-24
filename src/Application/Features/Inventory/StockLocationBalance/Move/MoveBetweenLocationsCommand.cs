namespace ZARI.Application.Features.Inventory.StockLocationBalances.Move;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record MoveBetweenLocationsCommand(
    Guid ItemId,
    Guid WarehouseId,
    string? BatchNo,
    Guid FromLocationId,
    Guid ToLocationId,
    decimal Qty) : ICommand<Result>;
