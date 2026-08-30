namespace ZARI.Application.Features.Sales.SalesInvoices.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The actual BIR-OR assignment + GL posting a Sales Invoice performs — this is the module's
/// highest-stakes handler, so it's hand-built rather than delegated. Extracted here (rather than
/// living inside ApproveSalesInvoiceCommandHandler) so both a quick-post Create and a normal
/// Approve run the exact same posting, same pattern as DeliveryPostingService established in
/// Wave 2. Unlike Delivery, nothing here runs its own retryable transaction/ChangeTracker.Clear()
/// (no stock engine is involved), so the caller can mutate the tracked entity directly and finish
/// with a plain SaveChangesAsync — mirrors ApInvoice's Approve exactly.
/// </summary>
internal static class SalesInvoicePostingService
{
    public static async Task<Result> PostAsync(
        IAppDbContext dbContext,
        ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        SalesInvoice invoice,
        CancellationToken cancellationToken)
    {
        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(invoice.BranchId, "BIR-OR"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure(numberResult.Error!);

        invoice.BirOrSeriesNumber = numberResult.Value!.DocumentNumber;

        return await PostInvoiceJournalAsync(dbContext, postGlJournalHandler, invoice, cancellationToken);
    }

    /// <summary>
    /// One balanced journal: Dr AR (Customer.ArAccountId ?? "1200") for the gross (VAT-inclusive,
    /// post-header-discount) total = Cr each line's Sales Revenue account (Item.SalesAccountId ??
    /// "4000") for its own VAT-exclusive net + Cr "2200" VAT Payable for the summed VAT. The header
    /// discount is applied uniformly (pro-rata) to every line's own post-line/statutory-discount
    /// net before VAT is extracted — the plan leaves the exact header-discount-vs-VAT interaction
    /// unspecified for v1, and applying it uniformly keeps every line's VAT math internally
    /// consistent rather than picking an arbitrary line to absorb it.
    /// </summary>
    private static async Task<Result> PostInvoiceJournalAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        SalesInvoice invoice,
        CancellationToken cancellationToken)
    {
        var headerDiscountPct = invoice.DiscountPct ?? 0;
        var revenueByAccount = new Dictionary<Guid, decimal>();
        decimal totalVat = 0;
        decimal totalAr = 0;

        foreach (var line in invoice.Lines)
        {
            var calc = SalesInvoiceLineCalculator.Calculate(new SalesInvoiceLineCalculator.LineInput(
                line.Qty, line.UnitPrice, line.DiscountPct, line.VatType, line.StatutoryDiscountType?.DiscountPct));

            var netAfterHeader = Math.Round(calc.NetAmount * (1 - headerDiscountPct / 100m), 4);
            var (netOfVat, vatAmount) = SalesInvoiceLineCalculator.SplitVat(netAfterHeader, calc.EffectiveVatType);

            var revenueAccountResult = Guid.TryParse(line.Item.SalesAccountId, out var explicitRevenueId)
                ? Result.Success(explicitRevenueId)
                : await GetDefaultAccountIdAsync(dbContext, "4000", "Sales Revenue", cancellationToken);
            if (!revenueAccountResult.IsSuccess)
                return Result.Failure(revenueAccountResult.Error!);

            revenueByAccount[revenueAccountResult.Value] = revenueByAccount.GetValueOrDefault(revenueAccountResult.Value) + netOfVat;
            totalVat += vatAmount;
            totalAr += netAfterHeader;
        }

        totalVat = Math.Round(totalVat, 4);
        totalAr = Math.Round(totalAr, 4);
        if (totalAr <= 0)
            return Result.Success();

        var arAccountResult = invoice.Customer.ArAccountId.HasValue
            ? Result.Success(invoice.Customer.ArAccountId.Value)
            : await GetDefaultAccountIdAsync(dbContext, "1200", "Accounts Receivable", cancellationToken);
        if (!arAccountResult.IsSuccess)
            return Result.Failure(arAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput> { new(arAccountResult.Value, invoice.CostCenterId, totalAr, 0, null) };
        lines.AddRange(revenueByAccount.Where(kv => kv.Value > 0)
            .Select(kv => new PostGlJournalLineInput(kv.Key, invoice.CostCenterId, 0, Math.Round(kv.Value, 4), null)));

        if (totalVat > 0)
        {
            var vatAccountResult = await GetDefaultAccountIdAsync(dbContext, "2200", "VAT Payable", cancellationToken);
            if (!vatAccountResult.IsSuccess)
                return Result.Failure(vatAccountResult.Error!);
            lines.Add(new PostGlJournalLineInput(vatAccountResult.Value, invoice.CostCenterId, 0, totalVat, null));
        }

        var description = $"Sales Invoice {invoice.InvoiceNo} — {invoice.Customer.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(invoice.BranchId, invoice.InvoiceDate, "SALES", "SalesInvoice", invoice.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private static async Task<Result<Guid>> GetDefaultAccountIdAsync(IAppDbContext dbContext, string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
