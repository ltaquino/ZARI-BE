namespace ZARI.Application.Features.Hr.PayrollRuns.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllPayrollRunsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllPayrollRunsQuery, Result<List<PayrollRunSummaryResponse>>>
{
    public async Task<Result<List<PayrollRunSummaryResponse>>> HandleAsync(GetAllPayrollRunsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAYROLL_RUNS", FormAction.View, cancellationToken))
            return Result.Failure<List<PayrollRunSummaryResponse>>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to view payroll runs."));

        var items = await dbContext.PayrollRuns.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PayrollRunSummaryResponse(r.Id, r.PayrollRunNo, r.BranchId, r.PayPeriodStart, r.PayPeriodEnd, r.PayDate, r.Status, r.Lines.Count, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
