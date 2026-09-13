namespace ZARI.Application.Features.Hr.PayrollRuns.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Domain.Common;

public sealed class GetPayrollRunQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetPayrollRunQuery, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(GetPayrollRunQuery query, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .Include(r => r.Lines).ThenInclude(l => l.LeaveTypeConsumed)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);
        if (run is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.View, run.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to view payroll runs for this branch."));

        return Result.Success(PayrollRunMapper.ToResponse(run));
    }
}
