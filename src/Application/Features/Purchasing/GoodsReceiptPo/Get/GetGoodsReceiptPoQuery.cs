namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record GetGoodsReceiptPoQuery(Guid Id) : IQuery<Result<GoodsReceiptPoResponse>>;
