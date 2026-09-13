namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllThirteenthMonthPayRunsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllThirteenthMonthPayRunsQuery, Result<List<ThirteenthMonthPayRunSummaryResponse>>>
{
    public async Task<Result<List<ThirteenthMonthPayRunSummaryResponse>>> HandleAsync(GetAllThirteenthMonthPayRunsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("13TH_MONTH_PAY_RUNS", FormAction.View, cancellationToken))
            return Result.Failure<List<ThirteenthMonthPayRunSummaryResponse>>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to view 13th month pay runs."));

        var items = await dbContext.ThirteenthMonthPayRuns.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ThirteenthMonthPayRunSummaryResponse(r.Id, r.RunNo, r.BranchId, r.Year, r.PayDate, r.Status, r.Lines.Count, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
