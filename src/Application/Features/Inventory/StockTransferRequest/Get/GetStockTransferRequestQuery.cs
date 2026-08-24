namespace ZARI.Application.Features.Inventory.StockTransferRequests.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record GetStockTransferRequestQuery(Guid Id) : IQuery<Result<StockTransferRequestResponse>>;
