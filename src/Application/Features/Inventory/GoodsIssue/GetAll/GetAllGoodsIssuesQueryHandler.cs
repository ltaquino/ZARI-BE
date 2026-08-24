namespace ZARI.Application.Features.Inventory.GoodsIssues.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGoodsIssuesQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllGoodsIssuesQuery, Result<List<GoodsIssueResponse>>>
{
    public async Task<Result<List<GoodsIssueResponse>>> HandleAsync(GetAllGoodsIssuesQuery query, CancellationToken cancellationToken = default)
    {
        var issues = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(i => i.GiDate)
            .ToListAsync(cancellationToken);

        return Result.Success(issues.Select(GoodsIssueMapper.ToResponse).ToList());
    }
}
