namespace ZARI.Application.Features.Sales.SalesInvoices.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// How much of a Sales Invoice has actually been paid — the sum of every POSTED Customer Payment's
/// line referencing it. Mirrors ApInvoicePaymentBalance (Purchasing), on the AR side. A payment
/// that's still DRAFT/PENDING_APPROVAL/PENDING_CANCELLATION never counts, so the payment currently
/// being created/approved/cancelled is always naturally excluded from these sums without needing an
/// explicit "exclude this one" filter — it isn't POSTED yet (or isn't POSTED any more) at the exact
/// moment each of those handlers checks this.
/// SalesInvoice.PaidAmount itself is left untouched — Wave 3 deliberately left that stored field
/// unused. Status is the only thing this module derives live from the payment ledger.
/// </summary>
internal static class SalesInvoicePaymentBalance
{
    /// <summary>
    /// The AR total actually posted for this invoice — the exact amount
    /// SalesInvoicePostingService.PostInvoiceJournalAsync posted to AR (its own totalAr
    /// accumulation), recomputed here rather than shared across the two files to avoid an
    /// artificial abstraction over a ~5-line loop.
    /// </summary>
    public static decimal GetInvoiceTotal(SalesInvoice invoice)
    {
        var headerDiscountPct = invoice.DiscountPct ?? 0;
        decimal totalAr = 0;

        foreach (var line in invoice.Lines)
        {
            var calc = SalesInvoiceLineCalculator.Calculate(new SalesInvoiceLineCalculator.LineInput(
                line.Qty, line.UnitPrice, line.DiscountPct, line.VatType, line.StatutoryDiscountType?.DiscountPct));

            var netAfterHeader = Math.Round(calc.NetAmount * (1 - headerDiscountPct / 100m), 4);
            totalAr += netAfterHeader;
        }

        return Math.Round(totalAr, 4);
    }

    public static Task<decimal> GetAmountPaidAsync(IAppDbContext dbContext, Guid salesInvoiceId, CancellationToken cancellationToken) =>
        dbContext.CustomerPaymentLines
            .Where(l => l.SalesInvoiceId == salesInvoiceId && l.CustomerPayment.Status == "POSTED")
            .SumAsync(l => l.AmountApplied, cancellationToken);

    public static async Task<Dictionary<Guid, decimal>> GetAmountsPaidAsync(
        IAppDbContext dbContext, IEnumerable<Guid> salesInvoiceIds, CancellationToken cancellationToken)
    {
        var ids = salesInvoiceIds.Distinct().ToList();
        var paid = await dbContext.CustomerPaymentLines
            .Where(l => ids.Contains(l.SalesInvoiceId) && l.CustomerPayment.Status == "POSTED")
            .GroupBy(l => l.SalesInvoiceId)
            .Select(g => new { SalesInvoiceId = g.Key, Amount = g.Sum(x => x.AmountApplied) })
            .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Amount, cancellationToken);
        return ids.ToDictionary(id => id, id => paid.GetValueOrDefault(id));
    }

    /// <summary>POSTED (nothing paid) -&gt; PARTIALLY_PAID (some paid) -&gt; PAID (fully paid).</summary>
    public static string DetermineStatus(decimal invoiceTotal, decimal amountPaid)
    {
        if (amountPaid <= 0) return "POSTED";
        return amountPaid >= invoiceTotal ? "PAID" : "PARTIALLY_PAID";
    }
}
