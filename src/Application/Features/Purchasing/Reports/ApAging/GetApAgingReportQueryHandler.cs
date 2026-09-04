namespace ZARI.Application.Features.Purchasing.Reports.ApAging;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Domain.Common;

/// <summary>
/// Real bug fix over the FE's original client-side computation (ApAgingReportPage.tsx): that version
/// aged the invoice's gross total and never subtracted payments, so a partially- or fully-paid
/// invoice still showed its full original amount as "outstanding". This version ages the actual
/// outstanding balance (invoiceTotal - amountPaid) instead, and excludes invoices that are already
/// fully paid off (nothing left to age). It also fixes a second, smaller bug: the FE only summed
/// `Lines`, silently reporting 0 for any EXPENSE-type invoice (whose amount lives in `ExpenseLines`
/// instead) — this version reuses ApInvoicePaymentBalance.GetInvoiceTotal, which already branches on
/// InvoiceType to sum whichever collection is actually populated.
/// </summary>
public sealed class GetApAgingReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetApAgingReportQuery, Result<ApAgingReportResponse>>
{
    public async Task<Result<ApAgingReportResponse>> HandleAsync(GetApAgingReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("AP_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<ApAgingReportResponse>(Error.Forbidden("ApAgingReport.Forbidden", "You do not have permission to view AP invoices."));

        var invoicesQuery = dbContext.ApInvoices.AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.ExpenseLines)
            .Where(i => i.Status == "POSTED");

        if (!string.IsNullOrWhiteSpace(query.BranchId)) invoicesQuery = invoicesQuery.Where(i => i.BranchId == query.BranchId);
        if (query.SupplierId is { } supplierId) invoicesQuery = invoicesQuery.Where(i => i.SupplierId == supplierId);

        var invoices = await invoicesQuery.ToListAsync(cancellationToken);

        var amountsPaid = await ApInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), cancellationToken);

        var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;

        var rows = invoices
            .Select(invoice => new
            {
                Invoice = invoice,
                Outstanding = ApInvoicePaymentBalance.GetInvoiceTotal(invoice) - amountsPaid.GetValueOrDefault(invoice.Id),
            })
            // A paid-off invoice has nothing left to age.
            .Where(x => x.Outstanding >= 0.01m)
            .Select(x =>
            {
                var dueDate = x.Invoice.DueDate ?? x.Invoice.InvoiceDate;
                var daysOverdue = (int)Math.Floor((asOfDate - dueDate).TotalDays);
                return new
                {
                    x.Invoice,
                    x.Outstanding,
                    DueDate = dueDate,
                    DaysOverdue = daysOverdue,
                    Bucket = BucketOf(daysOverdue),
                };
            })
            .OrderByDescending(x => x.DaysOverdue)
            .ToList();

        var groups = rows
            .GroupBy(x => x.Invoice.SupplierId)
            .Select(g =>
            {
                var first = g.First().Invoice;
                return new ApAgingSupplierGroup(
                    g.Key,
                    first.Supplier.Code,
                    first.Supplier.Name,
                    g.Sum(x => x.Outstanding),
                    g.Select(x => new ApAgingInvoiceRow(
                        x.Invoice.Id,
                        x.Invoice.InvoiceNo,
                        x.Invoice.SupplierInvoiceNo,
                        x.Invoice.BranchId,
                        x.DueDate,
                        x.DaysOverdue,
                        x.Bucket,
                        x.Outstanding)).ToList());
            })
            .OrderByDescending(g => g.GroupTotal)
            .ToList();

        var response = new ApAgingReportResponse(
            groups,
            rows.Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "current").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "1-30").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "31-60").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "61-90").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "90+").Sum(x => x.Outstanding));

        return Result.Success(response);
    }

    private static string BucketOf(int daysOverdue) => daysOverdue switch
    {
        <= 0 => "current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };
}
