namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAllPaged;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReceiptPosPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<GoodsReceiptPoResponse>>>;
