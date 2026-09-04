namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Sales Invoices — a generic data browser, not a BIR-compliance
/// report (that's Sales Book's job), so Amount is the plain per-line net total
/// (Qty x UnitPrice x (1 - DiscountPct/100)), not SalesInvoiceLineCalculator's full VAT-split
/// math; it's computed in memory after materializing (same as GetInventoryAsOfQueryHandler's own
/// documented "bounded, occasional-use report" precedent) so it's exposed as a display-only column
/// (Filterable/Sortable = false) rather than pushed into the SQL predicate/order-by.
/// </summary>
public sealed class SalesInvoicesReportDataset : IReportDataset
{
    public string Key => "SALES_INVOICES";
    public string Label => "Sales Invoices";
    public string RequiredPermissionCode => "SALES_INVOICES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("InvoiceNo", "Invoice No.", ReportFieldType.Text),
        new("InvoiceDate", "Invoice Date", ReportFieldType.Date),
        new("DueDate", "Due Date", ReportFieldType.Date),
        new("CustomerName", "Customer", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
        new("PaidAmount", "Paid Amount", ReportFieldType.Currency),
        new("Remarks", "Remarks", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<SalesInvoice> query = dbContext.SalesInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "InvoiceNo" => ReportDatasetFilters.Text(query, filter, i => i.InvoiceNo),
                "InvoiceDate" => ReportDatasetFilters.Date(query, filter, i => (DateTimeOffset?)i.InvoiceDate),
                "DueDate" => ReportDatasetFilters.Date(query, filter, i => i.DueDate),
                "CustomerName" => ReportDatasetFilters.Text(query, filter, i => i.Customer.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, i => i.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, i => i.Status),
                "PaidAmount" => ReportDatasetFilters.Decimal(query, filter, i => (decimal?)i.PaidAmount),
                "Remarks" => ReportDatasetFilters.Text(query, filter, i => i.Remarks),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "InvoiceNo" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.InvoiceNo),
            "InvoiceDate" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.InvoiceDate),
            "DueDate" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.DueDate),
            "CustomerName" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Customer.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Status),
            "PaidAmount" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.PaidAmount),
            _ => query.OrderByDescending(i => i.InvoiceDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var invoices = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = invoices.Select(i => BuildRow(i, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(SalesInvoice invoice, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "InvoiceNo" => invoice.InvoiceNo,
                "InvoiceDate" => invoice.InvoiceDate,
                "DueDate" => invoice.DueDate,
                "CustomerName" => invoice.Customer.Name,
                "BranchId" => invoice.BranchId,
                "Status" => invoice.Status,
                "Amount" => ComputeAmount(invoice),
                "PaidAmount" => invoice.PaidAmount,
                "Remarks" => invoice.Remarks,
                _ => null,
            };
        }
        return row;
    }

    private static decimal ComputeAmount(SalesInvoice invoice) =>
        Math.Round(invoice.Lines.Sum(l => l.Qty * l.UnitPrice * (1 - l.DiscountPct / 100m)), 2);
}
