namespace ZARI.Application.Features.Purchasing.Reports.PurchaseBook;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Helpers;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Ported from the FE's PurchaseBookReportPage.tsx: every line's own billed amount (VAT-inclusive,
/// per the vendor's invoice) bucketed by its VatType — same VAT-extraction direction Sales Invoice
/// already uses (VatSplitter mirrors the FE's shared splitVat()).
/// </summary>
public sealed class GetPurchaseBookReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetPurchaseBookReportQuery, Result<PurchaseBookReportResponse>>
{
    public async Task<Result<PurchaseBookReportResponse>> HandleAsync(GetPurchaseBookReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("AP_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<PurchaseBookReportResponse>(Error.Forbidden("PurchaseBookReport.Forbidden", "You do not have permission to view AP invoices."));

        var invoicesQuery = dbContext.ApInvoices.AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.ExpenseLines)
            .Where(i => i.Status == "POSTED");

        if (!string.IsNullOrWhiteSpace(query.BranchId)) invoicesQuery = invoicesQuery.Where(i => i.BranchId == query.BranchId);
        if (query.SupplierId is { } supplierId) invoicesQuery = invoicesQuery.Where(i => i.SupplierId == supplierId);

        var invoices = await invoicesQuery
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var rows = invoices.Select(BucketInvoice).ToList();

        var response = new PurchaseBookReportResponse(
            rows,
            rows.Sum(r => r.Gross),
            rows.Sum(r => r.VatableSales),
            rows.Sum(r => r.ZeroRated),
            rows.Sum(r => r.Exempt),
            rows.Sum(r => r.InputTax));

        return Result.Success(response);
    }

    private static PurchaseBookRow BucketInvoice(ApInvoice invoice)
    {
        decimal gross = 0, vatableSales = 0, zeroRated = 0, exempt = 0, inputTax = 0;

        void AddLine(decimal amount, string vatType)
        {
            gross += amount;
            var (netOfVat, vatAmount) = VatSplitter.Split(amount, vatType);
            if (vatType == "VATABLE")
            {
                vatableSales += netOfVat;
                inputTax += vatAmount;
            }
            else if (vatType == "ZERO_RATED")
            {
                zeroRated += amount;
            }
            else
            {
                exempt += amount;
            }
        }

        foreach (var line in invoice.Lines) AddLine(line.Qty * line.UnitCost, line.VatType);
        foreach (var line in invoice.ExpenseLines) AddLine(line.Amount, line.VatType);

        return new PurchaseBookRow(
            invoice.Id,
            invoice.InvoiceDate,
            invoice.Supplier.Name,
            invoice.Supplier.TaxId,
            invoice.SupplierInvoiceNo,
            invoice.BranchId,
            gross,
            vatableSales,
            zeroRated,
            exempt,
            inputTax);
    }
}
