namespace ZARI.Application.Features.Loan.LoanPayments.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document — mirrors LoanDisbursement's ApproveCancellation: reverse the GL journal, unwind every
/// touched schedule line's PrincipalPaid/InterestPaid/PenaltyPaid/Status back to what it was before
/// this payment (using each LoanPaymentAllocation row's own recorded amounts, not re-derived),
/// revert the LoanAccount from FULLY_PAID to ACTIVE if this payment was the one that completed it,
/// then post a reversal LoanLedgerEntry. Decide the cancellation request first so a failed decide
/// never leaves anything reversed. PostLoanLedgerEntryCommand clears the ChangeTracker, so it runs
/// LAST — the schedule-line/account unwind (variable-count, so ExecuteUpdateAsync doesn't fit
/// cleanly) is saved via a plain SaveChangesAsync first, then only the final payment status flip
/// uses ExecuteUpdateAsync.
/// </summary>
public sealed class ApproveLoanPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanPaymentCancellationCommand, Result<LoanPaymentResponse>>
{
    public async Task<Result<LoanPaymentResponse>> HandleAsync(ApproveLoanPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.LoanPayments
            .Include(p => p.LoanAccount).ThenInclude(a => a.Customer)
            .Include(p => p.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CostCenter)
            .Include(p => p.Allocations).ThenInclude(a => a.LoanAmortizationScheduleLine)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("LoanPayment.NotFound", $"Loan payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_PAYMENTS", cancellationToken))
            return Result.Failure<LoanPaymentResponse>(Error.Forbidden("LoanPayment.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (payment.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanPaymentResponse>(Error.Validation("LoanPayment.NotPendingCancellation", "Only a loan payment pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan payment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(decideResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("LoanPayment", payment.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {payment.PaymentNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(reverseJournalsResult.Error!);

        var principalTotal = 0m;
        foreach (var allocation in payment.Allocations)
        {
            var line = allocation.LoanAmortizationScheduleLine;
            line.PrincipalPaid -= allocation.PrincipalApplied;
            line.InterestPaid -= allocation.InterestApplied;
            line.PenaltyPaid -= allocation.PenaltyApplied;
            line.Status = line.PrincipalPaid >= line.PrincipalDue && line.InterestPaid >= line.InterestDue
                ? "PAID"
                : line.PrincipalPaid > 0 || line.InterestPaid > 0 || line.PenaltyPaid > 0 ? "PARTIALLY_PAID" : "DUE";
            principalTotal += allocation.PrincipalApplied;
        }

        if (payment.LoanAccount.Status == "FULLY_PAID")
            payment.LoanAccount.Status = "ACTIVE";

        // Persists the schedule-line unwind and any LoanAccount status revert — before the ledger
        // call below clears the tracker.
        await dbContext.SaveChangesAsync(cancellationToken);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(payment.LoanAccountId, DateTimeOffset.UtcNow, "PAYMENT", "LoanPayment", payment.Id.ToString(),
                principalTotal, 0, $"Cancellation of {payment.PaymentNo}", true),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(ledgerResult.Error!);

        var cancelledAt = DateTimeOffset.UtcNow;
        payment.Status = "CANCELLED";
        payment.CancelledBy = command.ApproverUserId;
        payment.CancelledAt = cancelledAt;
        await dbContext.LoanPayments.Where(p => p.Id == payment.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, "CANCELLED")
                .SetProperty(p => p.CancelledBy, command.ApproverUserId)
                .SetProperty(p => p.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(notifyResult.Error!);

        return Result.Success(LoanPaymentMapper.ToResponse(payment));
    }
}
