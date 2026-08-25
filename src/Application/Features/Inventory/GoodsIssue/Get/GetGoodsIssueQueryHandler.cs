namespace ZARI.Application.Features.Inventory.GoodsIssues.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Domain.Common;

public sealed class GetGoodsIssueQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetGoodsIssueQuery, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(GetGoodsIssueQuery query, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == query.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{query.Id}' was not found."));

        var canViewSource = await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, issue.BranchId, cancellationToken);
        var canViewDest = issue.DestBranchId is not null &&
            await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.View, issue.DestBranchId, cancellationToken);
        if (!canViewSource && !canViewDest)
            return Result.Failure<GoodsIssueResponse>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to view this goods issue."));

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
