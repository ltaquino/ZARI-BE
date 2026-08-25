namespace ZARI.Application.Features.Purchasing.GoodsReturns.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record GetGoodsReturnQuery(Guid Id) : IQuery<Result<GoodsReturnResponse>>;
