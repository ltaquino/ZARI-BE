namespace ZARI.Application.Features.Loan.LoanWriteOffs.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// write-off — mirrors LoanDisbursement's cancellation-approval shape: reverse the GL journal, post
/// a reversal LoanLedgerEntry (PrincipalIn instead of PrincipalOut, restoring the running balance to
/// what it was), flip LoanAccount back to ACTIVE, then decide the cancellation request first so a
/// failed decide never leaves anything reversed. No "downstream activity" re-check is needed — see
/// RequestLoanWriteOffCancellationCommandHandler's doc comment for why nothing new could have
/// happened to the account while it sat WRITTEN_OFF / PENDING_CANCELLATION.
/// </summary>
public sealed class ApproveLoanWriteOffCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(ApproveLoanWriteOffCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_WRITE_OFFS", cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (writeOff.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotPendingCancellation", "Only a loan write-off pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_WRITE_OFF" && r.EntityId == writeOff.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan write-off."));

        // Decide before any reversal side-effect — a failed decide must leave nothing reversed yet,
        // so it stays retryable. Same ordering rule as ApproveLoanDisbursementCancellationCommandHandler.
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(decideResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("LoanWriteOff", writeOff.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {writeOff.WriteOffNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(reverseJournalsResult.Error!);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(writeOff.LoanAccountId, DateTimeOffset.UtcNow, "WRITE_OFF", "LoanWriteOff", writeOff.Id.ToString(),
                writeOff.Amount, 0, $"Cancellation of {writeOff.WriteOffNo}", true),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(ledgerResult.Error!);

        var cancelledAt = DateTimeOffset.UtcNow;
        await dbContext.LoanAccounts.Where(a => a.Id == writeOff.LoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "ACTIVE"), cancellationToken);
        await dbContext.LoanWriteOffs.Where(w => w.Id == writeOff.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(w => w.Status, "CANCELLED")
                .SetProperty(w => w.CancelledBy, command.ApproverUserId)
                .SetProperty(w => w.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        var saved = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstAsync(w => w.Id == writeOff.Id, cancellationToken);

        return Result.Success(LoanWriteOffMapper.ToResponse(saved));
    }
}
