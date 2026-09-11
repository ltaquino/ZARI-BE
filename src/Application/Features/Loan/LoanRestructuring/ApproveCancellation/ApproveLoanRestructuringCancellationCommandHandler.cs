namespace ZARI.Application.Features.Loan.LoanRestructurings.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// restructuring: un-supersedes every OLD-account schedule line this restructuring superseded
/// (recomputing DUE/PARTIALLY_PAID/PAID off its own paid amounts — never PAID, since only non-PAID
/// lines were ever superseded), cancels the spawned NEW loan account, reverts the OLD account back
/// to ACTIVE, and posts a reversal LoanLedgerEntry on each account netting the running balance back
/// to what it was. No GL journal to reverse (Approve never posted one). Decide the cancellation
/// request first so a failed decide never leaves anything reversed.
/// </summary>
public sealed class ApproveLoanRestructuringCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanRestructuringCancellationCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(ApproveLoanRestructuringCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.ScheduleLines)
            .Include(r => r.NewLoanAccount)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (restructuring.NewLoanAccountId is null)
            return Result.Failure<LoanRestructuringResponse>(Error.Failure("LoanRestructuring.MissingNewAccount", "This posted restructuring has no new loan account on record — data is in an unexpected state."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_RESTRUCTURINGS", cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (restructuring.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.NotPendingCancellation", "Only a loan restructuring pending cancellation can be cancelled this way."));

        // Authoritative re-check — a payment could have posted against the new account in the gap
        // between RequestCancellation and this approval.
        var hasPayments = await dbContext.LoanPayments.AnyAsync(p => p.LoanAccountId == restructuring.NewLoanAccountId && p.Status != "CANCELLED", cancellationToken);
        if (hasPayments)
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.HasDownstreamActivity", "The new loan account already has a payment posted against it — cancel or reverse those first."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_RESTRUCTURING" && r.EntityId == restructuring.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan restructuring."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(decideResult.Error!);

        foreach (var line in restructuring.OldLoanAccount.ScheduleLines.Where(l => l.Status == "SUPERSEDED"))
        {
            line.Status = line.PrincipalPaid >= line.PrincipalDue && line.InterestPaid >= line.InterestDue
                ? "PAID"
                : line.PrincipalPaid > 0 || line.InterestPaid > 0 || line.PenaltyPaid > 0 ? "PARTIALLY_PAID" : "DUE";
        }

        // Persists the un-superseded schedule lines — before the ledger calls below clear the tracker.
        await dbContext.SaveChangesAsync(cancellationToken);

        var oldLedgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(restructuring.OldLoanAccountId, DateTimeOffset.UtcNow, "RESTRUCTURING", "LoanRestructuring", $"{restructuring.Id}:OLD",
                restructuring.OldPrincipalBalance, 0, $"Cancellation of {restructuring.RestructuringNo}", true),
            cancellationToken);
        if (!oldLedgerResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(oldLedgerResult.Error!);

        var newLedgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(restructuring.NewLoanAccountId.Value, DateTimeOffset.UtcNow, "RESTRUCTURING", "LoanRestructuring", $"{restructuring.Id}:NEW",
                0, restructuring.OldPrincipalBalance, $"Cancellation of {restructuring.RestructuringNo}", true),
            cancellationToken);
        if (!newLedgerResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(newLedgerResult.Error!);

        var cancelledAt = DateTimeOffset.UtcNow;
        await dbContext.LoanAccounts.Where(a => a.Id == restructuring.OldLoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "ACTIVE"), cancellationToken);
        await dbContext.LoanAccounts.Where(a => a.Id == restructuring.NewLoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "CANCELLED"), cancellationToken);
        await dbContext.LoanRestructurings.Where(r => r.Id == restructuring.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "CANCELLED")
                .SetProperty(r => r.CancelledBy, command.ApproverUserId)
                .SetProperty(r => r.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        var saved = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstAsync(r => r.Id == restructuring.Id, cancellationToken);

        return Result.Success(LoanRestructuringMapper.ToResponse(saved));
    }
}
