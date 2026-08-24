namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Common;

public sealed record GetStockLocationTransferQuery(Guid Id) : IQuery<Result<StockLocationTransferResponse>>;
