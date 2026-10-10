namespace ZARI.Application.Features.Hr.PayrollRuns.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Posts ONE GL journal aggregated across every Line in the run (not
/// per-employee) — see HrModuleContext.md §3.13. Beyond the doc's own posting skeleton, this also
/// credits a "2350 Other Payroll Deductions Payable" clearing line for SUM(DeductionTotal) — the
/// skeleton's Dr/Cr set didn't otherwise balance once EmployeeDeduction amounts are non-zero, since
/// NetPay already nets them out but nothing credited the other side. The part that recovers money the
/// employee already owes (PayrollLine.ReceivableRecoveryDeduction — e.g. a route-sales trip shortage, which
/// the trip close debited to 1220) credits "1220 Due from Employees" instead, clearing that receivable.
/// Lines with a zero amount are omitted entirely (PostGlJournalCommand rejects a 0/0 line as malformed).
/// </summary>
public sealed class ApprovePayrollRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApprovePayrollRunCommand, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(ApprovePayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .Include(r => r.Lines).ThenInclude(l => l.LeaveTypeConsumed)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Approve, run.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to approve payroll runs for this branch."));

        if (run.Status != "PENDING_APPROVAL")
            return Result.Failure<PayrollRunResponse>(Error.Validation("PayrollRun.NotPendingApproval", "Only a payroll run pending approval can be approved."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "PAYROLL_RUN" && r.EntityId == run.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this payroll run."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(decideResult.Error!);

        var postResult = await PostPayrollJournalAsync(run, cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(postResult.Error!);

        // Compute only ever previewed each Line's leave consumption (never touching the ledger, so
        // re-running Compute while DRAFT stays side-effect-free) — Approve is the one point this
        // becomes permanent, exactly once, matching every other irreversible-side-effect-at-Approve
        // pattern in this codebase (GL posting included, right above this).
        foreach (var line in run.Lines.Where(l => l.LeaveDaysConsumed > 0 && l.LeaveTypeConsumedId is not null))
        {
            await LeaveBalanceLedgerService.PostEntryAsync(
                dbContext, line.EmployeeId, line.LeaveTypeConsumedId!.Value, run.PayDate, "USAGE", -line.LeaveDaysConsumed,
                "PayrollRuns", run.Id.ToString(), cancellationToken);
        }

        run.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PAYROLL_RUN", run.Id.ToString(), run.BranchId, "APPROVED", "ACTIVITY",
                "approved this payroll run", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(notifyResult.Error!);

        return Result.Success(PayrollRunMapper.ToResponse(run));
    }

    private async Task<Result> PostPayrollJournalAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var totalGrossPay = run.Lines.Sum(l => l.GrossPay);
        var totalSssEr = run.Lines.Sum(l => l.SssEmployerShare);
        var totalPhilHealthEr = run.Lines.Sum(l => l.PhilHealthEmployerShare);
        var totalPagIbigEr = run.Lines.Sum(l => l.PagIbigEmployerShare);
        var totalSssPayable = run.Lines.Sum(l => l.SssEmployeeShare + l.SssEmployerShare);
        var totalPhilHealthPayable = run.Lines.Sum(l => l.PhilHealthEmployeeShare + l.PhilHealthEmployerShare);
        var totalPagIbigPayable = run.Lines.Sum(l => l.PagIbigEmployeeShare + l.PagIbigEmployerShare);
        var totalWithholdingTax = run.Lines.Sum(l => l.WithholdingTax);
        var totalReceivableRecovery = run.Lines.Sum(l => l.ReceivableRecoveryDeduction);
        var totalDeductions = run.Lines.Sum(l => l.DeductionTotal) - totalReceivableRecovery;
        var totalNetPay = run.Lines.Sum(l => l.NetPay);

        var lines = new List<PostGlJournalLineInput>();

        async Task<Result> AddDebitAsync(string code, string label, decimal amount)
        {
            if (amount <= 0) return Result.Success();
            var accountResult = await GetAccountIdAsync(code, label, cancellationToken);
            if (!accountResult.IsSuccess) return Result.Failure(accountResult.Error!);
            lines.Add(new PostGlJournalLineInput(accountResult.Value, null, amount, 0, null));
            return Result.Success();
        }

        async Task<Result> AddCreditAsync(string code, string label, decimal amount)
        {
            if (amount <= 0) return Result.Success();
            var accountResult = await GetAccountIdAsync(code, label, cancellationToken);
            if (!accountResult.IsSuccess) return Result.Failure(accountResult.Error!);
            lines.Add(new PostGlJournalLineInput(accountResult.Value, null, 0, amount, null));
            return Result.Success();
        }

        // Each of these awaits a real dbContext query — they must run one at a time. Building an
        // array of the (already-started) tasks first and awaiting them afterward, as an earlier
        // version of this method did, kicks off all ten GlAccounts queries concurrently against the
        // same DbContext instance and throws "A second operation was started on this context
        // instance before a previous operation completed" — caught live, not by the unit tests
        // (EF InMemory tolerated it; only the real MySqlConnector provider enforced the rule).
        var debitResult = await AddDebitAsync("6020", "Salaries and Wages", totalGrossPay);
        if (!debitResult.IsSuccess) return debitResult;
        debitResult = await AddDebitAsync("6021", "Employer SSS Share Expense", totalSssEr);
        if (!debitResult.IsSuccess) return debitResult;
        debitResult = await AddDebitAsync("6022", "Employer PhilHealth Share Expense", totalPhilHealthEr);
        if (!debitResult.IsSuccess) return debitResult;
        debitResult = await AddDebitAsync("6023", "Employer Pag-IBIG Share Expense", totalPagIbigEr);
        if (!debitResult.IsSuccess) return debitResult;

        var creditResult = await AddCreditAsync("2300", "SSS Payable", totalSssPayable);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("2310", "PhilHealth Payable", totalPhilHealthPayable);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("2320", "Pag-IBIG Payable", totalPagIbigPayable);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("2330", "Withholding Tax Payable", totalWithholdingTax);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("2350", "Other Payroll Deductions Payable", totalDeductions);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("1220", "Due from Employees", totalReceivableRecovery);
        if (!creditResult.IsSuccess) return creditResult;
        creditResult = await AddCreditAsync("2340", "Net Pay Payable", totalNetPay);
        if (!creditResult.IsSuccess) return creditResult;

        var description = $"Payroll Run {run.PayrollRunNo} — {run.PayPeriodStart:yyyy-MM-dd} to {run.PayPeriodEnd:yyyy-MM-dd}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(run.BranchId, run.PayDate, "HR", "PayrollRun", run.Id.ToString(), description, lines),
            cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
