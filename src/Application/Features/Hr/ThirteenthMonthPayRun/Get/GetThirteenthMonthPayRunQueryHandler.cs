namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Domain.Common;

public sealed class GetThirteenthMonthPayRunQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetThirteenthMonthPayRunQuery, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(GetThirteenthMonthPayRunQuery query, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);
        if (run is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.View, run.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to view 13th month pay runs for this branch."));

        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(run));
    }
}
