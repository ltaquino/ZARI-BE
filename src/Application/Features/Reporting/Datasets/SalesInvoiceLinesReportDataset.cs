namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Sales Invoice Lines — a line-level drill-down companion to
/// SalesInvoicesReportDataset, for reports that need one row per item rather than one row per
/// invoice. LineAmount is the same plain per-line net total (Qty x UnitPrice x (1 - DiscountPct/100))
/// as SalesInvoicesReportDataset's own Amount, computed in memory after materializing (same
/// "bounded, occasional-use report" precedent as GetInventoryAsOfQueryHandler), so it's a
/// display-only column (Filterable/Sortable = false) rather than pushed into the SQL predicate/order-by.
/// </summary>
public sealed class SalesInvoiceLinesReportDataset : IReportDataset
{
    public string Key => "SALES_INVOICE_LINES";
    public string Label => "Sales Invoice Lines";
    public string RequiredPermissionCode => "SALES_INVOICES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("InvoiceNo", "Invoice No.", ReportFieldType.Text),
        new("InvoiceDate", "Invoice Date", ReportFieldType.Date),
        new("CustomerName", "Customer", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("ItemName", "Item", ReportFieldType.Text),
        new("Qty", "Qty", ReportFieldType.Number),
        new("UnitPrice", "Unit Price", ReportFieldType.Currency),
        new("DiscountPct", "Discount %", ReportFieldType.Number),
        new("LineAmount", "Line Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
        new("VatType", "VAT Type", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<SalesInvoiceLine> query = dbContext.SalesInvoiceLines.AsNoTracking()
            .Include(l => l.SalesInvoice).ThenInclude(i => i.Customer)
            .Include(l => l.Item);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "InvoiceNo" => ReportDatasetFilters.Text(query, filter, l => l.SalesInvoice.InvoiceNo),
                "InvoiceDate" => ReportDatasetFilters.Date(query, filter, l => (DateTimeOffset?)l.SalesInvoice.InvoiceDate),
                "CustomerName" => ReportDatasetFilters.Text(query, filter, l => l.SalesInvoice.Customer.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, l => l.SalesInvoice.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, l => l.SalesInvoice.Status),
                "ItemName" => ReportDatasetFilters.Text(query, filter, l => l.Item.Name),
                "Qty" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.Qty),
                "UnitPrice" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.UnitPrice),
                "DiscountPct" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.DiscountPct),
                "VatType" => ReportDatasetFilters.Text(query, filter, l => l.VatType),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "InvoiceNo" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.SalesInvoice.InvoiceNo),
            "InvoiceDate" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.SalesInvoice.InvoiceDate),
            "CustomerName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.SalesInvoice.Customer.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.SalesInvoice.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.SalesInvoice.Status),
            "ItemName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.Item.Name),
            "Qty" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.Qty),
            "UnitPrice" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.UnitPrice),
            "DiscountPct" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.DiscountPct),
            "VatType" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.VatType),
            _ => query.OrderByDescending(l => l.SalesInvoice.InvoiceDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var lines = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = lines.Select(l => BuildRow(l, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(SalesInvoiceLine line, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "InvoiceNo" => line.SalesInvoice.InvoiceNo,
                "InvoiceDate" => line.SalesInvoice.InvoiceDate,
                "CustomerName" => line.SalesInvoice.Customer.Name,
                "BranchId" => line.SalesInvoice.BranchId,
                "Status" => line.SalesInvoice.Status,
                "ItemName" => line.Item.Name,
                "Qty" => line.Qty,
                "UnitPrice" => line.UnitPrice,
                "DiscountPct" => line.DiscountPct,
                "LineAmount" => ComputeLineAmount(line),
                "VatType" => line.VatType,
                _ => null,
            };
        }
        return row;
    }

    private static decimal ComputeLineAmount(SalesInvoiceLine line) =>
        Math.Round(line.Qty * line.UnitPrice * (1 - line.DiscountPct / 100m), 2);
}
