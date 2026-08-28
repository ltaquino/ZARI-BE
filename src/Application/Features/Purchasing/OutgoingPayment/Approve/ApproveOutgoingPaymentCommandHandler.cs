namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. No stock movement — a payment never touches inventory. Converts
/// the "2000" Accounts Payable liability into an actual cash/bank outflow (Dr Accounts Payable,
/// Cr the selected bank/cash account's own GL account), then moves every referenced AP Invoice to
/// PARTIALLY_PAID or PAID depending on how much of its balance this payment actually covers. Each
/// invoice's eligibility and remaining balance are re-checked here (not just at Create/Update) to
/// close the race where two payments both draw against the same invoice's balance — whichever
/// approves first claims that part of the balance.
/// </summary>
public sealed class ApproveOutgoingPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveOutgoingPaymentCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(ApproveOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice).ThenInclude(i => i.Lines)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice).ThenInclude(i => i.ExpenseLines)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Approve, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to approve outgoing payments for this branch."));

        if (payment.Status != "PENDING_APPROVAL")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NotPendingApproval", "Only outgoing payments pending approval can be approved."));

        // Re-derive each invoice's next status from its balance right now (not what Create/Update
        // saw) — this payment isn't POSTED yet, so GetAmountPaidAsync naturally excludes it here.
        var newInvoiceStatuses = new Dictionary<Guid, string>();
        foreach (var line in payment.Lines)
        {
            var invoice = line.ApInvoice;
            if (invoice.Status is not ("POSTED" or "PARTIALLY_PAID"))
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.InvoiceNotPayable", $"AP invoice '{invoice.InvoiceNo}' is no longer eligible for payment — it may already be fully paid or cancelled."));

            var invoiceTotal = ApInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaidSoFar = await ApInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            if (line.Amount > invoiceTotal - amountPaidSoFar)
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.AmountExceedsBalance", $"AP invoice '{invoice.InvoiceNo}' no longer has enough remaining balance for this payment — it may have been paid down by another payment since this one was created."));

            newInvoiceStatuses[invoice.Id] = ApInvoicePaymentBalance.DetermineStatus(invoiceTotal, amountPaidSoFar + line.Amount);
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "OUTGOING_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this outgoing payment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(decideResult.Error!);

        var journalResult = await PostPaymentJournalAsync(payment, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(journalResult.Error!);

        payment.Status = "POSTED";
        foreach (var line in payment.Lines)
            line.ApInvoice.Status = newInvoiceStatuses[line.ApInvoice.Id];
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "APPROVED", "ACTIVITY",
                "approved this outgoing payment", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }

    private async Task<Result> PostPaymentJournalAsync(Domain.Entities.OutgoingPayment payment, CancellationToken cancellationToken)
    {
        var total = payment.Lines.Sum(l => l.Amount);
        if (total <= 0)
            return Result.Success();

        var apAccountResult = await ResolveApAccountIdAsync(payment.Supplier, cancellationToken);
        if (!apAccountResult.IsSuccess)
            return Result.Failure(apAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>
        {
            new(apAccountResult.Value, payment.CostCenterId, total, 0, null),
            new(payment.BankAccount.GlAccountId, payment.CostCenterId, 0, total, null)
        };

        var description = $"Outgoing Payment {payment.PaymentNo} — {payment.Supplier.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(payment.BranchId, payment.PaymentDate, "PURCHASING", "OutgoingPayment", payment.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }

    /// Uses the supplier's own AP control-account override if one is configured, falling back to the
    /// default "2000 Accounts Payable" — matches ApInvoice's own resolution so a supplier billed via
    /// an EXPENSE/ITEM invoice against a custom AP account is paid off against that same account.
    private Task<Result<Guid>> ResolveApAccountIdAsync(Domain.Entities.Supplier supplier, CancellationToken cancellationToken) =>
        supplier.ApAccountId.HasValue
            ? Task.FromResult(Result.Success(supplier.ApAccountId.Value))
            : GetDefaultAccountIdAsync("2000", "Accounts Payable", cancellationToken);
}
