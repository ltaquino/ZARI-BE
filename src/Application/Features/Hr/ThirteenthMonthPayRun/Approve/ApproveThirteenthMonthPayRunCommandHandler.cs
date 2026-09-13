namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Posts ONE GL journal aggregated across every Line in the run — same
/// "one journal per run, aggregated across employees" shape as ApprovePayrollRunCommandHandler.
/// </summary>
public sealed class ApproveThirteenthMonthPayRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(ApproveThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Approve, run.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to approve 13th month pay runs for this branch."));

        if (run.Status != "PENDING_APPROVAL")
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Validation("ThirteenthMonthPayRun.NotPendingApproval", "Only a 13th month pay run pending approval can be approved."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "THIRTEENTH_MONTH_PAY_RUN" && r.EntityId == run.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this 13th month pay run."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(decideResult.Error!);

        var postResult = await PostThirteenthMonthPayJournalAsync(run, cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(postResult.Error!);

        run.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("THIRTEENTH_MONTH_PAY_RUN", run.Id.ToString(), run.BranchId, "APPROVED", "ACTIVITY",
                "approved this 13th month pay run", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(notifyResult.Error!);

        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(run));
    }

    private async Task<Result> PostThirteenthMonthPayJournalAsync(ThirteenthMonthPayRun run, CancellationToken cancellationToken)
    {
        var totalThirteenthMonthPay = run.Lines.Sum(l => l.ThirteenthMonthPay);
        if (totalThirteenthMonthPay <= 0)
            return Result.Failure(Error.Validation("ThirteenthMonthPayRun.NothingToPost", "This run has no 13th month pay to post."));

        var expenseAccountResult = await GetAccountIdAsync("6026", "13th Month Pay Expense", cancellationToken);
        if (!expenseAccountResult.IsSuccess) return Result.Failure(expenseAccountResult.Error!);

        var payableAccountResult = await GetAccountIdAsync("2340", "Net Pay Payable", cancellationToken);
        if (!payableAccountResult.IsSuccess) return Result.Failure(payableAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>
        {
            new(expenseAccountResult.Value, null, totalThirteenthMonthPay, 0, null),
            new(payableAccountResult.Value, null, 0, totalThirteenthMonthPay, null)
        };

        var description = $"13th Month Pay Run {run.RunNo} — {run.Year}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(run.BranchId, run.PayDate, "HR", "ThirteenthMonthPayRuns", run.Id.ToString(), description, lines),
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
