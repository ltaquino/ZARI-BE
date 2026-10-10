namespace ZARI.Application.Features.Hr.PayrollRuns.Compute;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT -&gt; DRAFT: (re)generates every Lines row for the run from scratch. Safely re-runnable any
/// number of times while the run is still DRAFT — see HrModuleContext.md §3.13 and
/// PayrollComputationService's own header comment for the exact per-employee calculation and its
/// v1 assumptions.
/// </summary>
public sealed class ComputePayrollRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<ComputePayrollRunCommand, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(ComputePayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to compute payroll runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure<PayrollRunResponse>(Error.Validation("PayrollRun.NotDraft", "Only a draft payroll run can be computed."));

        var eligibleEmployees = await dbContext.Employees
            .Where(e => e.BranchId == run.BranchId && e.Status == "active"
                && e.DateHired <= run.PayPeriodEnd
                && (e.SeparationDate == null || e.SeparationDate >= run.PayPeriodStart))
            .ToListAsync(cancellationToken);

        dbContext.PayrollLines.RemoveRange(run.Lines);

        foreach (var employee in eligibleEmployees)
        {
            var draft = await PayrollComputationService.ComputeForEmployeeAsync(dbContext, employee, run, cancellationToken);
            dbContext.PayrollLines.Add(new PayrollLine
            {
                PayrollRunId = run.Id,
                EmployeeId = draft.EmployeeId,
                BasicPay = draft.BasicPay,
                OvertimePay = draft.OvertimePay,
                AllowanceTotal = draft.AllowanceTotal,
                LeaveDaysConsumed = draft.LeaveDaysConsumed,
                LeaveTypeConsumedId = draft.LeaveTypeConsumedId,
                AbsenceDeduction = draft.AbsenceDeduction,
                GrossPay = draft.GrossPay,
                SssEmployeeShare = draft.SssEmployeeShare,
                SssEmployerShare = draft.SssEmployerShare,
                PhilHealthEmployeeShare = draft.PhilHealthEmployeeShare,
                PhilHealthEmployerShare = draft.PhilHealthEmployerShare,
                PagIbigEmployeeShare = draft.PagIbigEmployeeShare,
                PagIbigEmployerShare = draft.PagIbigEmployerShare,
                WithholdingTax = draft.WithholdingTax,
                DeductionTotal = draft.DeductionTotal,
                ReceivableRecoveryDeduction = draft.ReceivableRecoveryDeduction,
                NetPay = draft.NetPay
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .Include(r => r.Lines).ThenInclude(l => l.LeaveTypeConsumed)
            .FirstAsync(r => r.Id == run.Id, cancellationToken);
        return Result.Success(PayrollRunMapper.ToResponse(reloaded));
    }
}
