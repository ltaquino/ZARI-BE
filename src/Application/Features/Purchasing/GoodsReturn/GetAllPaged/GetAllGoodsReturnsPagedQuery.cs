namespace ZARI.Application.Features.Purchasing.GoodsReturns.GetAllPaged;

using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReturnsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<GoodsReturnResponse>>>;
