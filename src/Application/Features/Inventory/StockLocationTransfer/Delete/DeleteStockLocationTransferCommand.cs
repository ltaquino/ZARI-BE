namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteStockLocationTransferCommand(Guid Id) : ICommand<Result>;
