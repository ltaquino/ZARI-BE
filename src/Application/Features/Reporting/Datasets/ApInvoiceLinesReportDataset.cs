namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over AP Invoice Lines — a line-level drill-down companion to
/// ApInvoicesReportDataset, for reports that need one row per received item rather than one row
/// per invoice. Only the item-level Lines collection (every ITEM-type AP invoice's own lines) —
/// EXPENSE-type invoices bill via a separate ApInvoiceExpenseLines collection entirely, and that
/// expense-line reporting is already covered by the existing Purchase Book report, so it's
/// deliberately left out of this dataset. ApInvoiceLine has no discount/variance field (unlike
/// SalesInvoiceLine's DiscountPct), so LineAmount is simply Qty x UnitCost — computed in memory
/// after materializing (same "bounded, occasional-use report" precedent as
/// GetInventoryAsOfQueryHandler), so it's a display-only column (Filterable/Sortable = false)
/// rather than pushed into the SQL predicate/order-by.
/// </summary>
public sealed class ApInvoiceLinesReportDataset : IReportDataset
{
    public string Key => "AP_INVOICE_LINES";
    public string Label => "AP Invoice Lines";
    public string RequiredPermissionCode => "AP_INVOICES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("InvoiceNo", "Invoice No.", ReportFieldType.Text),
        new("InvoiceDate", "Invoice Date", ReportFieldType.Date),
        new("SupplierName", "Supplier", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("ItemName", "Item", ReportFieldType.Text),
        new("Qty", "Qty", ReportFieldType.Number),
        new("UnitCost", "Unit Cost", ReportFieldType.Currency),
        new("LineAmount", "Line Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ApInvoiceLine> query = dbContext.ApInvoiceLines.AsNoTracking()
            .Include(l => l.ApInvoice).ThenInclude(i => i.Supplier)
            .Include(l => l.Item);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "InvoiceNo" => ReportDatasetFilters.Text(query, filter, l => l.ApInvoice.InvoiceNo),
                "InvoiceDate" => ReportDatasetFilters.Date(query, filter, l => (DateTimeOffset?)l.ApInvoice.InvoiceDate),
                "SupplierName" => ReportDatasetFilters.Text(query, filter, l => l.ApInvoice.Supplier.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, l => l.ApInvoice.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, l => l.ApInvoice.Status),
                "ItemName" => ReportDatasetFilters.Text(query, filter, l => l.Item.Name),
                "Qty" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.Qty),
                "UnitCost" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.UnitCost),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "InvoiceNo" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ApInvoice.InvoiceNo),
            "InvoiceDate" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ApInvoice.InvoiceDate),
            "SupplierName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ApInvoice.Supplier.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ApInvoice.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ApInvoice.Status),
            "ItemName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.Item.Name),
            "Qty" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.Qty),
            "UnitCost" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.UnitCost),
            _ => query.OrderByDescending(l => l.ApInvoice.InvoiceDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var lines = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = lines.Select(l => BuildRow(l, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(ApInvoiceLine line, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "InvoiceNo" => line.ApInvoice.InvoiceNo,
                "InvoiceDate" => line.ApInvoice.InvoiceDate,
                "SupplierName" => line.ApInvoice.Supplier.Name,
                "BranchId" => line.ApInvoice.BranchId,
                "Status" => line.ApInvoice.Status,
                "ItemName" => line.Item.Name,
                "Qty" => line.Qty,
                "UnitCost" => line.UnitCost,
                "LineAmount" => Math.Round(line.Qty * line.UnitCost, 2),
                _ => null,
            };
        }
        return row;
    }
}
