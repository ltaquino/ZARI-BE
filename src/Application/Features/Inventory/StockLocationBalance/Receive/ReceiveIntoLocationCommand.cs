namespace ZARI.Application.Features.Inventory.StockLocationBalances.Receive;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record ReceiveIntoLocationCommand(Guid ItemId, Guid WarehouseId, Guid LocationId, string? BatchNo, decimal Qty) : ICommand<Result>;
