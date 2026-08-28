namespace ZARI.Application.Features.Purchasing.ApInvoices.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;

/// <summary>
/// How much of an AP Invoice has actually been paid — the sum of every POSTED Outgoing Payment's
/// line referencing it. A payment that's still DRAFT/PENDING_APPROVAL/PENDING_CANCELLATION never
/// counts, so the payment currently being created/approved/cancelled is always naturally excluded
/// from these sums without needing an explicit "exclude this one" filter — it isn't POSTED yet (or
/// isn't POSTED any more) at the exact moment each of those handlers checks this.
/// Shared between the AP Invoice read side (to display a running balance) and the Outgoing Payment
/// write side (to cap a new payment's amount and to (re)determine an invoice's status).
/// </summary>
internal static class ApInvoicePaymentBalance
{
    /// <summary>
    /// Total invoiced amount — branches on invoice type since ITEM invoices keep their amount in
    /// `Lines` (Qty x UnitCost) while EXPENSE invoices keep it in `ExpenseLines` (Amount) instead;
    /// `Lines` is always empty for an EXPENSE invoice, so summing it alone silently gives 0.
    /// </summary>
    public static decimal GetInvoiceTotal(ZARI.Domain.Entities.ApInvoice invoice) =>
        invoice.InvoiceType == "EXPENSE"
            ? invoice.ExpenseLines.Sum(l => Math.Round(l.Amount, 4))
            : invoice.Lines.Sum(l => Math.Round(l.Qty * l.UnitCost, 4));

    public static Task<decimal> GetAmountPaidAsync(IAppDbContext dbContext, Guid apInvoiceId, CancellationToken cancellationToken) =>
        dbContext.OutgoingPaymentLines
            .Where(l => l.ApInvoiceId == apInvoiceId && l.OutgoingPayment.Status == "POSTED")
            .SumAsync(l => l.Amount, cancellationToken);

    public static async Task<Dictionary<Guid, decimal>> GetAmountsPaidAsync(
        IAppDbContext dbContext, IEnumerable<Guid> apInvoiceIds, CancellationToken cancellationToken)
    {
        var ids = apInvoiceIds.Distinct().ToList();
        var paid = await dbContext.OutgoingPaymentLines
            .Where(l => ids.Contains(l.ApInvoiceId) && l.OutgoingPayment.Status == "POSTED")
            .GroupBy(l => l.ApInvoiceId)
            .Select(g => new { ApInvoiceId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.ApInvoiceId, x => x.Amount, cancellationToken);
        return ids.ToDictionary(id => id, id => paid.GetValueOrDefault(id));
    }

    /// <summary>POSTED (nothing paid) -&gt; PARTIALLY_PAID (some paid) -&gt; PAID (fully paid).</summary>
    public static string DetermineStatus(decimal invoiceTotal, decimal amountPaid)
    {
        if (amountPaid <= 0) return "POSTED";
        return amountPaid >= invoiceTotal ? "PAID" : "PARTIALLY_PAID";
    }
}
