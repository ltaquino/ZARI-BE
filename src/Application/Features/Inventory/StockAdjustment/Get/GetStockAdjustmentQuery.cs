namespace ZARI.Application.Features.Inventory.StockAdjustments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record GetStockAdjustmentQuery(Guid Id) : IQuery<Result<StockAdjustmentResponse>>;
