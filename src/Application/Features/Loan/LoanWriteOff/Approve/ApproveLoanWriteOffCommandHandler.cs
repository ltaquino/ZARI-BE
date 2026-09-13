namespace ZARI.Application.Features.Loan.LoanWriteOffs.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Requires elevated HQ-only authority
/// (IPermissionService.HasHqApprovalAuthorityAsync), unlike every other Loan document's Approve
/// step which only needs branch-scoped CanApprove — see LoanWriteOff's class doc comment for why.
/// Posts Dr WriteOffExpenseAccount / Cr the account's Loans Receivable account, records a
/// principal-out LoanLedgerEntry zeroing the running balance, then flips the LoanAccount
/// ACTIVE -> WRITTEN_OFF. PostLoanLedgerEntryCommand clears the ChangeTracker, so the trailing
/// status flips use ExecuteUpdateAsync rather than tracked mutation + SaveChangesAsync (same
/// gotcha as every other Loan Approve handler).
/// </summary>
public sealed class ApproveLoanWriteOffCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanWriteOffCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(ApproveLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasHqApprovalAuthorityAsync("LOAN_WRITE_OFFS", cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "Only someone with approve permission assigned to the head office branch can approve a loan write-off."));

        if (writeOff.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotPendingApproval", "Only a loan write-off pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: the
        // loan account could have been restructured, fully paid, or cancelled in the gap between
        // Submit and Approve.
        if (writeOff.LoanAccount.Status != "ACTIVE")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.AccountNotActive", "The loan account is no longer active."));

        if (writeOff.LoanAccount.LoanReceivableAccountId is not { } loanReceivableAccountId)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("GlAccount.NotFound", "No Loans Receivable account is configured for this loan account or its loan product."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_WRITE_OFF" && r.EntityId == writeOff.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan write-off."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(decideResult.Error!);

        // Never trust the possibly-stale Create/Update-time snapshot — payments could have posted
        // since. Same rule as LoanRestructuring's Approve.
        var principalBalance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, writeOff.LoanAccountId, cancellationToken);
        if (principalBalance <= 0)
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NoBalance", "This loan account has no outstanding principal balance to write off."));

        var description = $"Loan Write-off {writeOff.WriteOffNo} — {writeOff.LoanAccount.LoanAcctNo} ({writeOff.LoanAccount.Customer.Name})";
        var lines = new List<PostGlJournalLineInput>
        {
            new(writeOff.WriteOffExpenseAccountId, null, principalBalance, 0, null),
            new(loanReceivableAccountId, null, 0, principalBalance, null)
        };
        var journalResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(writeOff.BranchId, writeOff.WriteOffDate, "LOAN", "LoanWriteOff", writeOff.Id.ToString(), description, lines),
            cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(journalResult.Error!);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(writeOff.LoanAccountId, writeOff.WriteOffDate, "WRITE_OFF", "LoanWriteOff", writeOff.Id.ToString(),
                0, principalBalance, $"Write-off {writeOff.WriteOffNo}", false),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(ledgerResult.Error!);

        await dbContext.LoanAccounts.Where(a => a.Id == writeOff.LoanAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "WRITTEN_OFF"), cancellationToken);
        await dbContext.LoanWriteOffs.Where(w => w.Id == writeOff.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(w => w.Status, "POSTED")
                .SetProperty(w => w.Amount, principalBalance), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "APPROVED", "ACTIVITY",
                "approved this loan write-off", command.ApproverUserId),
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
