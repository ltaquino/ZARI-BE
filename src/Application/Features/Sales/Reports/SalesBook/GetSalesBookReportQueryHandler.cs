namespace ZARI.Application.Features.Sales.Reports.SalesBook;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// BIR Sales Book: broader inclusion than Purchase Book's POSTED-only rule — a Sales Invoice that's
/// PENDING_APPROVAL/PENDING_CANCELLATION/PARTIALLY_PAID/PAID etc. already carries a BIR-OR number
/// and must still appear, so only DRAFT (never posted) and CANCELLED (voided) are excluded. Reuses
/// SalesInvoiceLineCalculator.Calculate + SplitVat exactly as the live invoice/receipt posting path
/// does, so the report always ties out to what was actually invoiced — no parallel VAT math.
/// </summary>
public sealed class GetSalesBookReportQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetSalesBookReportQuery, Result<SalesBookReportResponse>>
{
    public async Task<Result<SalesBookReportResponse>> HandleAsync(GetSalesBookReportQuery query, CancellationToken cancellationToken = default)
    {
        var invoices = await dbContext.SalesInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .Where(i => i.Status != "DRAFT" && i.Status != "CANCELLED"
                && (query.BranchId == null || i.BranchId == query.BranchId))
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var rows = invoices.Select(BuildRow).ToList();

        var response = new SalesBookReportResponse(
            rows,
            Math.Round(rows.Sum(r => r.Gross), 2),
            Math.Round(rows.Sum(r => r.VatableSales), 2),
            Math.Round(rows.Sum(r => r.ZeroRated), 2),
            Math.Round(rows.Sum(r => r.Exempt), 2),
            Math.Round(rows.Sum(r => r.VatAmount), 2));

        return Result.Success(response);
    }

    private static SalesBookRow BuildRow(SalesInvoice invoice)
    {
        var headerDiscountPct = invoice.DiscountPct ?? 0;
        decimal vatable = 0, zeroRated = 0, exempt = 0, vatAmount = 0;

        foreach (var line in invoice.Lines)
        {
            var calc = SalesInvoiceLineCalculator.Calculate(new SalesInvoiceLineCalculator.LineInput(
                line.Qty, line.UnitPrice, line.DiscountPct, line.VatType, line.StatutoryDiscountType?.DiscountPct));

            var netAfterHeader = Math.Round(calc.NetAmount * (1 - headerDiscountPct / 100m), 4);
            var (netOfVat, vat) = SalesInvoiceLineCalculator.SplitVat(netAfterHeader, calc.EffectiveVatType);

            switch (calc.EffectiveVatType)
            {
                case "VATABLE":
                    vatable += netOfVat;
                    vatAmount += vat;
                    break;
                case "ZERO_RATED":
                    zeroRated += netOfVat;
                    break;
                default: // VAT_EXEMPT
                    exempt += netOfVat;
                    break;
            }
        }

        var gross = vatable + vatAmount + zeroRated + exempt;

        return new SalesBookRow(
            invoice.Id,
            invoice.InvoiceDate,
            invoice.Customer.Name,
            invoice.BirOrSeriesNumber,
            invoice.InvoiceNo,
            invoice.BranchId,
            Math.Round(gross, 2),
            Math.Round(vatable, 2),
            Math.Round(zeroRated, 2),
            Math.Round(exempt, 2),
            Math.Round(vatAmount, 2));
    }
}
