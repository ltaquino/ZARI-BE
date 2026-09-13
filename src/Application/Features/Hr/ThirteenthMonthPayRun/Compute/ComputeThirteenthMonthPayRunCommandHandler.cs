namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Compute;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT -&gt; DRAFT: (re)generates every Lines row from scratch, safely re-runnable any number of
/// times while still DRAFT — same shape as ComputePayrollRunCommandHandler. Only employees with at
/// least one POSTED PayrollLine within Year get a Line (an employee hired after the year, or one
/// whose payroll was never run/posted, has nothing to compute).
/// </summary>
public sealed class ComputeThirteenthMonthPayRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<ComputeThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(ComputeThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to compute 13th month pay runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Validation("ThirteenthMonthPayRun.NotDraft", "Only a draft 13th month pay run can be computed."));

        var eligibleEmployeeIds = await dbContext.PayrollLines
            .Where(l => l.Employee.BranchId == run.BranchId
                && l.PayrollRun.Status == "POSTED"
                && l.PayrollRun.PayPeriodStart.Year == run.Year)
            .Select(l => l.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        dbContext.ThirteenthMonthPayLines.RemoveRange(run.Lines);

        foreach (var employeeId in eligibleEmployeeIds)
        {
            var (totalBasicPay, thirteenthMonthPay) = await ThirteenthMonthPayComputationService.ComputeForEmployeeAsync(dbContext, employeeId, run.Year, cancellationToken);
            dbContext.ThirteenthMonthPayLines.Add(new ThirteenthMonthPayLine
            {
                ThirteenthMonthPayRunId = run.Id,
                EmployeeId = employeeId,
                TotalBasicPay = totalBasicPay,
                ThirteenthMonthPay = thirteenthMonthPay
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstAsync(r => r.Id == run.Id, cancellationToken);
        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(reloaded));
    }
}
