namespace ZARI.Application.Features.Inventory.GoodsIssues.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsIssuesPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsIssuesPagedQuery, Result<PagedResult<GoodsIssueResponse>>>
{
    public async Task<Result<PagedResult<GoodsIssueResponse>>> HandleAsync(GetAllGoodsIssuesPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_ISSUES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<GoodsIssueResponse>>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to view goods issues."));

        var baseQuery = dbContext.GoodsIssues.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.GiNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var issues = await baseQuery
            .OrderByDescending(i => i.GiDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GoodsIssueResponse>(issues.Select(GoodsIssueMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
