namespace ZARI.Application.Features.Inventory.GoodsIssues.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsIssuesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGoodsIssuesQuery, Result<List<GoodsIssueResponse>>>
{
    public async Task<Result<List<GoodsIssueResponse>>> HandleAsync(GetAllGoodsIssuesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_ISSUES", FormAction.View, cancellationToken))
            return Result.Failure<List<GoodsIssueResponse>>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to view goods issues."));

        var issues = await dbContext.GoodsIssues.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(i => i.GiDate)
            .ToListAsync(cancellationToken);

        return Result.Success(issues.Select(GoodsIssueMapper.ToResponse).ToList());
    }
}
