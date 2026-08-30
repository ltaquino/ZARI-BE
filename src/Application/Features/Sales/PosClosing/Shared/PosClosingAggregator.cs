namespace ZARI.Application.Features.Sales.PosClosing.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;

/// <summary>
/// The cutoff + aggregation logic shared by X-Reading (read-only) and Z-Reading (persists a
/// ZReading + increments Branch.ZCounter) — both run the exact same math, X-Reading just doesn't
/// write anything.
///
/// Cutoff is by BIR-OR number range, not calendar day: this branch's most recent ZReading's
/// LastOrNumber is the floor; every POSTED SalesInvoice with a BirOrSeriesNumber strictly greater
/// than that floor is "in range". Since DocumentSequence formats BIR-OR numbers as a constant
/// per-branch prefix ("000-") plus a fixed 6-digit zero-padded number (see AppDbSeeder's
/// DocumentSequence seed and GetNextDocumentNumberCommandHandler's formatting), ordinal string
/// comparison sorts identically to numeric order as long as that prefix/width never changes.
///
/// Rather than trying to push the string-range filter into SQL (which plain LINQ-to-EF can't
/// translate for CompareOrdinal reliably), this pulls every POSTED, numbered invoice for the
/// branch into memory and does the precise range filter client-side. Dataset per branch is a
/// bounded, moderate-sized set (invoices since the last close), so the simpler, always-correct
/// approach beats a coarse SQL pre-filter that risks excluding a legitimately in-range row.
/// </summary>
internal static class PosClosingAggregator
{
    public sealed record AggregationResult(
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int InvoiceCount,
        string? FirstOrNumber,
        string? LastOrNumber,
        decimal GrossSales,
        decimal TotalDiscounts,
        decimal VatableSales,
        decimal VatAmount,
        decimal VatExemptSales,
        decimal ZeroRatedSales,
        decimal NetSales);

    public static async Task<AggregationResult> AggregateAsync(
        IAppDbContext dbContext, string branchId, DateTimeOffset periodEnd, CancellationToken cancellationToken)
    {
        var lastZReading = await dbContext.ZReadings
            .Where(z => z.BranchId == branchId)
            .OrderByDescending(z => z.ZCounterValue)
            .FirstOrDefaultAsync(cancellationToken);

        var floorOrNumber = lastZReading?.LastOrNumber;

        var candidates = await dbContext.SalesInvoices
            .Where(i => i.BranchId == branchId && i.Status == "POSTED" && i.BirOrSeriesNumber != null)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .ToListAsync(cancellationToken);

        var inRange = candidates
            .Where(i => floorOrNumber == null || string.CompareOrdinal(i.BirOrSeriesNumber, floorOrNumber) > 0)
            .OrderBy(i => i.BirOrSeriesNumber, StringComparer.Ordinal)
            .ToList();

        // No prior ZReading for this branch: fall back to the earliest InvoiceDate in the closed
        // set as a display-only period start, or periodEnd itself for a genuinely empty first-ever
        // reading (there's nothing to derive a start from).
        var periodStart = lastZReading?.PeriodEnd
            ?? (inRange.Count > 0 ? inRange.Min(i => i.InvoiceDate) : periodEnd);

        decimal grossSales = 0, totalDiscounts = 0, vatableSales = 0, vatAmount = 0, vatExemptSales = 0, zeroRatedSales = 0;

        foreach (var invoice in inRange)
        {
            var headerDiscountPct = invoice.DiscountPct ?? 0;
            foreach (var line in invoice.Lines)
            {
                var calc = SalesInvoiceLineCalculator.Calculate(new SalesInvoiceLineCalculator.LineInput(
                    line.Qty, line.UnitPrice, line.DiscountPct, line.VatType, line.StatutoryDiscountType?.DiscountPct));

                var netAfterHeader = Math.Round(calc.NetAmount * (1 - headerDiscountPct / 100m), 4);
                var (netOfVat, lineVat) = SalesInvoiceLineCalculator.SplitVat(netAfterHeader, calc.EffectiveVatType);

                grossSales += calc.GrossAmount;
                totalDiscounts += calc.DiscountAmount + (calc.NetAmount - netAfterHeader);

                switch (calc.EffectiveVatType)
                {
                    case "VATABLE":
                        vatableSales += netOfVat;
                        vatAmount += lineVat;
                        break;
                    case "VAT_EXEMPT":
                        vatExemptSales += netAfterHeader;
                        break;
                    default: // ZERO_RATED
                        zeroRatedSales += netAfterHeader;
                        break;
                }
            }
        }

        grossSales = Math.Round(grossSales, 4);
        totalDiscounts = Math.Round(totalDiscounts, 4);
        vatableSales = Math.Round(vatableSales, 4);
        vatAmount = Math.Round(vatAmount, 4);
        vatExemptSales = Math.Round(vatExemptSales, 4);
        zeroRatedSales = Math.Round(zeroRatedSales, 4);
        var netSales = vatableSales + vatExemptSales + zeroRatedSales;

        return new AggregationResult(
            periodStart,
            periodEnd,
            inRange.Count,
            inRange.Count > 0 ? inRange[0].BirOrSeriesNumber : null,
            inRange.Count > 0 ? inRange[^1].BirOrSeriesNumber : null,
            grossSales,
            totalDiscounts,
            vatableSales,
            vatAmount,
            vatExemptSales,
            zeroRatedSales,
            netSales);
    }
}
