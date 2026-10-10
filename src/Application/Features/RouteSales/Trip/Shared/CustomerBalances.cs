namespace ZARI.Application.Features.RouteSales.Trips.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;

/// <summary>One unpaid invoice — the handheld allocates a collection to these, oldest first.</summary>
public sealed record OpenInvoice(Guid SalesInvoiceId, string InvoiceNo, string? BirOrSeriesNumber, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, decimal Balance);

/// <summary>What each customer still owes on posted invoices — what the credit limit is checked against.</summary>
internal static class CustomerBalances
{
    /// <summary>Each customer's unpaid invoices, oldest first.</summary>
    public static async Task<Dictionary<Guid, List<OpenInvoice>>> OpenInvoicesAsync(IAppDbContext dbContext, IReadOnlyCollection<Guid> customerIds, CancellationToken ct)
    {
        var invoices = await dbContext.SalesInvoices.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .Where(i => customerIds.Contains(i.CustomerId) && (i.Status == "POSTED" || i.Status == "PARTIALLY_PAID"))
            .ToListAsync(ct);
        var paid = await SalesInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), ct);

        var result = customerIds.ToDictionary(id => id, _ => new List<OpenInvoice>());
        foreach (var invoice in invoices.OrderBy(i => i.InvoiceDate).ThenBy(i => i.InvoiceNo))
        {
            var balance = Math.Max(0, SalesInvoicePaymentBalance.GetInvoiceTotal(invoice) - paid.GetValueOrDefault(invoice.Id));
            if (balance > 0)
                result[invoice.CustomerId].Add(new OpenInvoice(invoice.Id, invoice.InvoiceNo, invoice.BirOrSeriesNumber, invoice.InvoiceDate, invoice.DueDate, balance));
        }
        return result;
    }

    public static async Task<Dictionary<Guid, decimal>> OpenArAsync(IAppDbContext dbContext, IReadOnlyCollection<Guid> customerIds, CancellationToken ct)
    {
        var invoices = await dbContext.SalesInvoices.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .Where(i => customerIds.Contains(i.CustomerId) && (i.Status == "POSTED" || i.Status == "PARTIALLY_PAID"))
            .ToListAsync(ct);
        var paid = await SalesInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), ct);

        var result = customerIds.ToDictionary(id => id, _ => 0m);
        foreach (var invoice in invoices)
            result[invoice.CustomerId] += Math.Max(0, SalesInvoicePaymentBalance.GetInvoiceTotal(invoice) - paid.GetValueOrDefault(invoice.Id));
        return result;
    }
}
