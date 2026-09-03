namespace ZARI.Application.Features.Sales.CustomerPayments.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The actual GL posting + invoice-status re-derivation a Customer Payment performs — extracted
/// here (rather than living inside ApproveCustomerPaymentCommandHandler) so both a quick-post
/// Create and a normal Approve run the exact same posting, same pattern as
/// SalesInvoicePostingService/DeliveryPostingService. No stock movement, no BIR-OR assignment (a
/// payment isn't itself a BIR-facing receipt) — just the AR-to-cash journal and each referenced
/// invoice's PARTIALLY_PAID/PAID transition. Nothing here runs its own retryable
/// transaction/ChangeTracker.Clear() (no stock engine involved), so the caller can mutate the
/// tracked entities directly and finish with a plain SaveChangesAsync — mirrors OutgoingPayment's
/// Approve exactly, flipped to the AR side.
/// </summary>
internal static class CustomerPaymentPostingService
{
    /// <summary>
    /// Re-derives each referenced invoice's next status from its balance right now (not what
    /// Create/Update saw) — this payment isn't POSTED yet, so GetAmountPaidAsync naturally excludes
    /// it here. Callable both before an Approve decides its ApprovalRequest (the authoritative,
    /// one-shot-safe re-check) and directly from a quick-post Create (which has no ApprovalRequest
    /// at all).
    /// </summary>
    public static async Task<Result<Dictionary<Guid, string>>> ComputeNewInvoiceStatusesAsync(
        IAppDbContext dbContext, CustomerPayment payment, CancellationToken cancellationToken)
    {
        var newInvoiceStatuses = new Dictionary<Guid, string>();
        foreach (var line in payment.Lines)
        {
            var invoice = line.SalesInvoice;
            if (invoice.Status is not ("POSTED" or "PARTIALLY_PAID"))
                return Result.Failure<Dictionary<Guid, string>>(Error.Validation("CustomerPayment.InvoiceNotPayable", $"Sales invoice '{invoice.InvoiceNo}' is no longer eligible for payment — it may already be fully paid or cancelled."));

            var invoiceTotal = SalesInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaidSoFar = await SalesInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            if (line.AmountApplied > invoiceTotal - amountPaidSoFar)
                return Result.Failure<Dictionary<Guid, string>>(Error.Validation("CustomerPayment.AmountExceedsBalance", $"Sales invoice '{invoice.InvoiceNo}' no longer has enough remaining balance for this payment — it may have been paid down by another payment since this one was created."));

            newInvoiceStatuses[invoice.Id] = SalesInvoicePaymentBalance.DetermineStatus(invoiceTotal, amountPaidSoFar + line.AmountApplied);
        }

        return Result.Success(newInvoiceStatuses);
    }

    /// <summary>
    /// One balanced journal: Cr Customer.ArAccountId ?? "1200" Accounts Receivable for the payment's
    /// total. The debit side either splits per split-tender line (POS Mode — Dr each tender's own
    /// PaymentMethod.GlAccountId, grouped/summed by account) when Tenders is non-empty, or falls
    /// back to the original single Dr CashAccountId line (Wave 4's own behavior) otherwise.
    /// </summary>
    public static async Task<Result> PostPaymentJournalAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        CustomerPayment payment,
        CancellationToken cancellationToken)
    {
        var total = payment.Lines.Sum(l => l.AmountApplied);
        if (total <= 0)
            return Result.Success();

        var arAccountResult = payment.Customer.ArAccountId.HasValue
            ? Result.Success(payment.Customer.ArAccountId.Value)
            : await GetDefaultAccountIdAsync(dbContext, "1200", "Accounts Receivable", cancellationToken);
        if (!arAccountResult.IsSuccess)
            return Result.Failure(arAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>();
        if (payment.Tenders.Count > 0)
        {
            var debitsByAccount = new Dictionary<Guid, decimal>();
            foreach (var tender in payment.Tenders)
                debitsByAccount[tender.PaymentMethod.GlAccountId] = debitsByAccount.GetValueOrDefault(tender.PaymentMethod.GlAccountId) + tender.Amount;

            lines.AddRange(debitsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, payment.CostCenterId, kv.Value, 0, null)));
        }
        else
        {
            lines.Add(new PostGlJournalLineInput(payment.CashAccountId, payment.CostCenterId, total, 0, null));
        }

        lines.Add(new PostGlJournalLineInput(arAccountResult.Value, payment.CostCenterId, 0, total, null));

        var description = $"Customer Payment {payment.PaymentNo} — {payment.Customer.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(payment.BranchId, payment.PaymentDate, "SALES", "CustomerPayment", payment.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    /// <summary>
    /// Full quick-post path: re-check + post the journal + flip the payment and every referenced
    /// invoice to their new status, in one call. Used only by a quick-post Create — Approve instead
    /// calls ComputeNewInvoiceStatusesAsync/PostPaymentJournalAsync separately around its own
    /// DecideApprovalRequestCommand, since that step must happen strictly between the two.
    /// </summary>
    public static async Task<Result> PostAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        CustomerPayment payment,
        CancellationToken cancellationToken)
    {
        var statusesResult = await ComputeNewInvoiceStatusesAsync(dbContext, payment, cancellationToken);
        if (!statusesResult.IsSuccess)
            return Result.Failure(statusesResult.Error!);

        var journalResult = await PostPaymentJournalAsync(dbContext, postGlJournalHandler, payment, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure(journalResult.Error!);

        payment.Status = "POSTED";
        foreach (var line in payment.Lines)
            line.SalesInvoice.Status = statusesResult.Value![line.SalesInvoiceId];

        return Result.Success();
    }

    private static async Task<Result<Guid>> GetDefaultAccountIdAsync(IAppDbContext dbContext, string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
