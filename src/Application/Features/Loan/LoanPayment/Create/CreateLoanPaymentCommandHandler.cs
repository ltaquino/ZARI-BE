namespace ZARI.Application.Features.Loan.LoanPayments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Quick-posts immediately — no DRAFT/Submit/Approve step (see LoanPayment's own class doc
/// comment). Allocates the tendered amount oldest-installment-first via
/// LoanPaymentAllocationEngine, saves the resulting schedule-line/allocation mutations, posts a
/// balanced GL journal (Dr the funding PaymentMethod's account, Cr Loans Receivable/Interest
/// Income/Penalty Income as applicable), records the payment on the account's append-only
/// LoanLedgerEntry, and — if this payment fully retires every installment — flips the LoanAccount
/// ACTIVE -> FULLY_PAID. PostLoanLedgerEntryCommand runs its own retryable transaction and calls
/// ChangeTracker.Clear() (same gotcha as LoanDisbursement's Approve), so it's called LAST and the
/// two trailing status flips use ExecuteUpdateAsync rather than a tracked mutation + SaveChangesAsync
/// — everything else (the schedule-line mutations, which vary in count and can't cleanly go through
/// ExecuteUpdateAsync) is instead saved via a plain SaveChangesAsync BEFORE the ledger call.
/// </summary>
public sealed class CreateLoanPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>> postLedgerEntryHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLoanPaymentCommand, Result<LoanPaymentResponse>>
{
    public async Task<Result<LoanPaymentResponse>> HandleAsync(CreateLoanPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_PAYMENTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<LoanPaymentResponse>(Error.Forbidden("LoanPayment.Forbidden", "You do not have permission to create loan payments for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == command.LoanAccountId, cancellationToken);
        if (account is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.LoanAccountId}' was not found."));

        if (account.Status != "ACTIVE")
            return Result.Failure<LoanPaymentResponse>(Error.Validation("LoanPayment.AccountNotActive", "Only an active loan account can receive a payment."));

        var paymentMethod = await dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == command.PaymentMethodId, cancellationToken);
        if (paymentMethod is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{command.PaymentMethodId}' was not found."));

        CostCenter? costCenter = null;
        if (command.CostCenterId is { } costCenterId)
        {
            costCenter = await dbContext.CostCenters.FirstOrDefaultAsync(c => c.Id == costCenterId, cancellationToken);
            if (costCenter is null)
                return Result.Failure<LoanPaymentResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{costCenterId}' was not found."));
        }

        var allocation = LoanPaymentAllocationEngine.Allocate(account.ScheduleLines, command.PaymentDate, command.Amount, account.PenaltyRatePct, account.GracePeriodDays);

        if (allocation.TotalOutstanding <= 0)
            return Result.Failure<LoanPaymentResponse>(Error.Validation("LoanPayment.NoOutstandingBalance", "This loan account has no outstanding balance to pay."));

        if (command.Amount > allocation.TotalOutstanding + 0.01m)
        {
            return Result.Failure<LoanPaymentResponse>(Error.Validation(
                "LoanPayment.AmountExceedsOutstanding",
                $"The payment amount ({command.Amount}) exceeds this loan account's total outstanding balance of {allocation.TotalOutstanding} — partial prepayment beyond what's currently due isn't supported yet."));
        }

        if (allocation.PrincipalTotal > 0 && account.LoanReceivableAccountId is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("GlAccount.NotFound", "No Loans Receivable account is configured for this loan account or its loan product."));
        if (allocation.InterestTotal > 0 && account.InterestIncomeAccountId is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("GlAccount.NotFound", "No Interest Income account is configured for this loan account or its loan product."));
        if (allocation.PenaltyTotal > 0 && account.PenaltyIncomeAccountId is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("GlAccount.NotFound", "No Penalty Income account is configured for this loan account or its loan product."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "LOAN-PMT"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(numberResult.Error!);

        var payment = new LoanPayment
        {
            PaymentNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            LoanAccountId = command.LoanAccountId,
            LoanAccount = account,
            PaymentDate = command.PaymentDate,
            Amount = command.Amount,
            PaymentMethodId = command.PaymentMethodId,
            PaymentMethod = paymentMethod,
            ReferenceNo = command.ReferenceNo,
            CostCenterId = command.CostCenterId,
            CostCenter = costCenter,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Allocations = allocation.Lines.Select(a => new LoanPaymentAllocation
            {
                LoanAmortizationScheduleLineId = a.Line.Id,
                LoanAmortizationScheduleLine = a.Line,
                PrincipalApplied = a.PrincipalApplied,
                InterestApplied = a.InterestApplied,
                PenaltyApplied = a.PenaltyApplied
            }).ToList()
        };

        dbContext.LoanPayments.Add(payment);
        // Persists the payment, its allocations, AND the schedule lines the allocation engine just
        // mutated in-place — all before the ledger call below clears the tracker.
        await dbContext.SaveChangesAsync(cancellationToken);

        var description = $"Loan Payment {payment.PaymentNo} — {account.LoanAcctNo} ({account.Customer.Name})";
        var journalLines = new List<PostGlJournalLineInput> { new(paymentMethod.GlAccountId, command.CostCenterId, command.Amount, 0, null) };
        if (allocation.PrincipalTotal > 0)
            journalLines.Add(new PostGlJournalLineInput(account.LoanReceivableAccountId!.Value, command.CostCenterId, 0, allocation.PrincipalTotal, null));
        if (allocation.InterestTotal > 0)
            journalLines.Add(new PostGlJournalLineInput(account.InterestIncomeAccountId!.Value, command.CostCenterId, 0, allocation.InterestTotal, null));
        if (allocation.PenaltyTotal > 0)
            journalLines.Add(new PostGlJournalLineInput(account.PenaltyIncomeAccountId!.Value, command.CostCenterId, 0, allocation.PenaltyTotal, null));

        var journalResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(command.BranchId, command.PaymentDate, "LOAN", "LoanPayment", payment.Id.ToString(), description, journalLines),
            cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(journalResult.Error!);

        var ledgerResult = await postLedgerEntryHandler.HandleAsync(
            new PostLoanLedgerEntryCommand(account.Id, command.PaymentDate, "PAYMENT", "LoanPayment", payment.Id.ToString(),
                0, allocation.PrincipalTotal, $"Payment {payment.PaymentNo}", false),
            cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(ledgerResult.Error!);

        // PostLoanLedgerEntryCommand clears the ChangeTracker (see class doc comment) — both status
        // flips below use ExecuteUpdateAsync rather than a tracked mutation + SaveChangesAsync.
        var isFullyPaid = account.ScheduleLines.All(l => l.Status == "PAID");
        payment.Status = "POSTED";
        await dbContext.LoanPayments.Where(p => p.Id == payment.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, "POSTED"), cancellationToken);
        if (isFullyPaid)
        {
            account.Status = "FULLY_PAID";
            await dbContext.LoanAccounts.Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "FULLY_PAID"), cancellationToken);
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_PAYMENT", payment.Id.ToString(), payment.BranchId, "CREATED", "ACTIVITY",
                isFullyPaid ? "posted this loan payment — the loan is now fully paid" : "posted this loan payment", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(notifyResult.Error!);

        return Result.Success(LoanPaymentMapper.ToResponse(payment));
    }
}
