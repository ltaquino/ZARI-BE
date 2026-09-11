namespace ZARI.Application.Features.Loan.LoanDisbursements.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document — mirrors GoodsReceiptPo's cancellation-approval shape: reverse the GL journal, post a
/// reversal LoanLedgerEntry (PrincipalOut instead of PrincipalIn, netting the running balance back
/// to what it was), flip LoanAccount back to PENDING_DISBURSEMENT, then decide the cancellation
/// request first so a failed decide never leaves anything reversed.
/// </summary>
public sealed class ApproveLoanDisbursementCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(ApproveLoanDisbursementCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_DISBURSEMENTS", cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (disbursement.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotPendingCancellation", "Only a loan disbursement pending cancellation can be cancelled this way."));

        // Authoritative re-check — a payment could have posted against this account in the gap
        // between RequestCancellation and this approval.
        var hasPayments = await dbContext.LoanPayments.AnyAsync(p => p.LoanAccountId == disbursement.LoanAccountId && p.Status != "CANCELLED", cancellationToken);
        if (hasPayments)
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.HasDownstreamActivity", "This loan account already has a payment posted against it — cancel or reverse those first."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_DISBURSEMENT" && r.EntityId == disbursement.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan disbursement."));

        // Decide before any reversal side-effect — a failed decide must leave nothing reversed yet,
        // so it stays retryable. Same ordering rule as ApproveGoodsReceiptPoCancellationCommandHandler.
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(decideResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("LoanDisbursement", disbursement.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {disbursement.DisbursementNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(reverseJournalsResult.Error!);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(disbursement.LoanAccountId, DateTimeOffset.UtcNow, "DISBURSEMENT", "LoanDisbursement", disbursement.Id.ToString(),
                0, disbursement.Amount, $"Cancellation of {disbursement.DisbursementNo}", true),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(ledgerResult.Error!);

        // PostLoanLedgerEntryCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() — that detaches `disbursement`/its LoanAccount, so ExecuteUpdateAsync
        // is needed here instead of a tracked mutation + SaveChangesAsync (same as the Approve handler).
        var cancelledAt = DateTimeOffset.UtcNow;
        disbursement.Status = "CANCELLED";
        disbursement.CancelledBy = command.ApproverUserId;
        disbursement.CancelledAt = cancelledAt;
        disbursement.LoanAccount.Status = "PENDING_DISBURSEMENT";
        await dbContext.LoanDisbursements.Where(d => d.Id == disbursement.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, "CANCELLED")
                .SetProperty(d => d.CancelledBy, command.ApproverUserId)
                .SetProperty(d => d.CancelledAt, cancelledAt), cancellationToken);
        await dbContext.LoanAccounts.Where(a => a.Id == disbursement.LoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "PENDING_DISBURSEMENT"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
