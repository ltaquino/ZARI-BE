namespace ZARI.Application.Features.Inventory.GoodsIssues.GetAllPaged;

using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsIssuesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<GoodsIssueResponse>>>;
