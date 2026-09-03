namespace ZARI.Application.Features.Inventory.GoodsReceipts.GetAllPaged;

using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReceiptsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<GoodsReceiptResponse>>>;
