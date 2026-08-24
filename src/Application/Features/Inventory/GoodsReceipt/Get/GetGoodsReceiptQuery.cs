namespace ZARI.Application.Features.Inventory.GoodsReceipts.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record GetGoodsReceiptQuery(Guid Id) : IQuery<Result<GoodsReceiptResponse>>;
