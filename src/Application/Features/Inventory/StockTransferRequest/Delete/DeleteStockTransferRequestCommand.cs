namespace ZARI.Application.Features.Inventory.StockTransferRequests.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteStockTransferRequestCommand(Guid Id) : ICommand<Result>;
