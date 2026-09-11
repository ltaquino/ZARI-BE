namespace ZARI.Application.Features.Loan.LoanRestructurings.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. The actual roll-forward: generates a brand-new LoanAccount (same
/// customer/product/branch/GL accounts, ACTIVE immediately — no disbursement step, since no new cash
/// moves) with a fresh amortization schedule at the new terms, for the OLD account's current
/// outstanding principal (re-derived fresh off the ledger here, never the possibly-stale Create-time
/// snapshot); supersedes every remaining installment on the OLD account's schedule; flips the OLD
/// account RESTRUCTURED. No GL journal — total principal owed doesn't change, only its repayment
/// terms, so the shared Loans Receivable balance is already correctly stated. Two LoanLedgerEntry
/// rows record the roll-forward for audit continuity (PrincipalOut on the old account's ledger,
/// PrincipalIn on the new one) — given distinct ReferenceId suffixes (":OLD"/":NEW") since
/// PostLoanLedgerEntryCommand's idempotency guard keys only on (ReferenceTable, ReferenceId,
/// IsReversal), not LoanAccountId, and both entries share this restructuring as their reference.
/// PostLoanLedgerEntryCommand clears the ChangeTracker, so it runs LAST (twice) — the new
/// account/schedule and the old schedule's SUPERSEDED flips are saved via a plain SaveChangesAsync
/// first, and only the trailing single-column status flips use ExecuteUpdateAsync.
/// </summary>
public sealed class ApproveLoanRestructuringCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanRestructuringCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(ApproveLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.ScheduleLines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Approve, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to approve loan restructurings for this branch."));

        if (restructuring.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.NotPendingApproval", "Only a loan restructuring pending approval can be approved."));

        var oldAccount = restructuring.OldLoanAccount;

        // Authoritative re-checks, closing the race a friendly Create/Update-time check can't.
        if (oldAccount.Status != "ACTIVE")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.AccountNotActive", "The loan account is no longer active."));

        var unpaidArrears = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(oldAccount, restructuring.RestructureDate);
        if (unpaidArrears > 0.01m)
        {
            return Result.Failure<LoanRestructuringResponse>(Error.Validation(
                "LoanRestructuring.ArrearsMustBeSettled",
                $"This account still has {unpaidArrears:N2} of unpaid interest/penalty — settle it with a loan payment first before approving this restructuring."));
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_RESTRUCTURING" && r.EntityId == restructuring.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan restructuring."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(decideResult.Error!);

        var principalBalance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, oldAccount.Id, cancellationToken);

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(restructuring.BranchId, "LOAN-ACCT"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(numberResult.Error!);

        var scheduleDrafts = AmortizationScheduleGenerator.Generate(
            principalBalance, restructuring.NewAnnualInterestRatePct, restructuring.NewTermMonths, restructuring.NewRepaymentFrequency, restructuring.NewFirstDueDate);

        var newAccount = new LoanAccount
        {
            LoanAcctNo = numberResult.Value!.DocumentNumber,
            BranchId = restructuring.BranchId,
            CustomerId = oldAccount.CustomerId,
            LoanProductId = oldAccount.LoanProductId,
            LoanApplicationId = null,
            PrincipalAmount = principalBalance,
            AnnualInterestRatePct = restructuring.NewAnnualInterestRatePct,
            TermMonths = restructuring.NewTermMonths,
            RepaymentFrequency = restructuring.NewRepaymentFrequency,
            GracePeriodDays = restructuring.NewGracePeriodDays,
            PenaltyRatePct = restructuring.NewPenaltyRatePct,
            GrantDate = restructuring.RestructureDate,
            FirstDueDate = restructuring.NewFirstDueDate,
            Status = "ACTIVE",
            Remarks = $"Restructured from {oldAccount.LoanAcctNo} — {restructuring.Reason}",
            LoanReceivableAccountId = oldAccount.LoanReceivableAccountId,
            InterestIncomeAccountId = oldAccount.InterestIncomeAccountId,
            PenaltyIncomeAccountId = oldAccount.PenaltyIncomeAccountId,
            CreatedBy = command.ApproverUserId,
            ScheduleLines = scheduleDrafts.Select(s => new LoanAmortizationScheduleLine
            {
                InstallmentNo = s.InstallmentNo,
                DueDate = s.DueDate,
                PrincipalDue = s.PrincipalDue,
                InterestDue = s.InterestDue,
                TotalDue = s.PrincipalDue + s.InterestDue,
                OutstandingPrincipalAfter = s.OutstandingPrincipalAfter,
                PrincipalPaid = 0,
                InterestPaid = 0,
                Status = "DUE"
            }).ToList()
        };
        dbContext.LoanAccounts.Add(newAccount);

        foreach (var line in oldAccount.ScheduleLines.Where(l => l.Status != "PAID"))
            line.Status = "SUPERSEDED";

        // Persists the new account + its schedule, and the old schedule's SUPERSEDED flips — before
        // the ledger calls below clear the tracker.
        await dbContext.SaveChangesAsync(cancellationToken);

        var oldLedgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(oldAccount.Id, restructuring.RestructureDate, "RESTRUCTURING", "LoanRestructuring", $"{restructuring.Id}:OLD",
                0, principalBalance, $"Restructured into {newAccount.LoanAcctNo}", false),
            cancellationToken);
        if (!oldLedgerResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(oldLedgerResult.Error!);

        var newLedgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(newAccount.Id, restructuring.RestructureDate, "RESTRUCTURING", "LoanRestructuring", $"{restructuring.Id}:NEW",
                principalBalance, 0, $"Restructured from {oldAccount.LoanAcctNo}", false),
            cancellationToken);
        if (!newLedgerResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(newLedgerResult.Error!);

        await dbContext.LoanAccounts.Where(a => a.Id == oldAccount.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "RESTRUCTURED"), cancellationToken);
        await dbContext.LoanRestructurings.Where(r => r.Id == restructuring.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "POSTED")
                .SetProperty(r => r.NewLoanAccountId, newAccount.Id)
                .SetProperty(r => r.OldPrincipalBalance, principalBalance), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "APPROVED", "ACTIVITY",
                $"approved this loan restructuring — new loan account {newAccount.LoanAcctNo}", command.ApproverUserId),
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
