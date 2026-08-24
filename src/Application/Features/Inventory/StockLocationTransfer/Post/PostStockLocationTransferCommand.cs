namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Post;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Common;

public sealed record PostStockLocationTransferCommand(Guid Id, string PostedBy) : ICommand<Result<StockLocationTransferResponse>>;
