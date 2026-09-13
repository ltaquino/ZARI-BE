namespace ZARI.Application.Features.Loan.LoanDisbursements.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Posts Dr Loans Receivable / Cr the funding PaymentMethod's GL
/// account, records the disbursement on the LoanAccount's append-only LoanLedgerEntry, then flips
/// the LoanAccount PENDING_DISBURSEMENT -> ACTIVE — the one place in the module so far where a
/// document's own approval also changes another entity's status, same shape as GoodsReceiptPo's
/// Approve (receives stock + posts GRNI). PostLoanLedgerEntryCommand runs its own retryable
/// transaction and calls ChangeTracker.Clear() (same as GRPO's ReceiveStockCommand), so both status
/// flips after it use ExecuteUpdateAsync rather than a tracked mutation + SaveChangesAsync.
/// </summary>
public sealed class ApproveLoanDisbursementCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanDisbursementCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(ApproveLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Approve, disbursement.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to approve loan disbursements for this branch."));

        if (disbursement.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotPendingApproval", "Only a loan disbursement pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create-time check can't: the loan
        // account could have been cancelled (or, if this ever becomes possible, disbursed by
        // another in-flight request) in the gap between Submit and Approve.
        if (disbursement.LoanAccount.Status != "PENDING_DISBURSEMENT")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.AccountNotPendingDisbursement", "The loan account is no longer pending disbursement."));

        if (disbursement.LoanAccount.LoanReceivableAccountId is not { } loanReceivableAccountId)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("GlAccount.NotFound", "No Loans Receivable account is configured for this loan account or its loan product."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_DISBURSEMENT" && r.EntityId == disbursement.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan disbursement."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(decideResult.Error!);

        var description = $"Loan Disbursement {disbursement.DisbursementNo} — {disbursement.LoanAccount.LoanAcctNo} ({disbursement.LoanAccount.Customer.Name})";
        var lines = new List<PostGlJournalLineInput>
        {
            new(loanReceivableAccountId, disbursement.CostCenterId, disbursement.Amount, 0, null),
            new(disbursement.PaymentMethod.GlAccountId, disbursement.CostCenterId, 0, disbursement.Amount, null)
        };
        var journalResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(disbursement.BranchId, disbursement.DisbursementDate, "LOAN", "LoanDisbursement", disbursement.Id.ToString(), description, lines),
            cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(journalResult.Error!);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(disbursement.LoanAccountId, disbursement.DisbursementDate, "DISBURSEMENT", "LoanDisbursement", disbursement.Id.ToString(),
                disbursement.Amount, 0, $"Disbursement {disbursement.DisbursementNo}", false),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(ledgerResult.Error!);

        // PostLoanLedgerEntryCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `disbursement`
        // (and its LoanAccount) this handler loaded earlier, so mutating them and calling
        // SaveChangesAsync would silently persist nothing. ExecuteUpdateAsync writes directly,
        // independent of the tracker — same gotcha as ApproveGoodsReceiptPoCommandHandler's status
        // flip after ReceiveStockCommand.
        disbursement.Status = "POSTED";
        disbursement.LoanAccount.Status = "ACTIVE";
        await dbContext.LoanDisbursements.Where(d => d.Id == disbursement.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, "POSTED"), cancellationToken);
        await dbContext.LoanAccounts.Where(a => a.Id == disbursement.LoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "ACTIVE"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "APPROVED", "ACTIVITY",
                "approved this loan disbursement", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
