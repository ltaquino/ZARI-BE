namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over AP Invoices. Amount and OutstandingBalance both reuse
/// ApInvoicePaymentBalance (the same helper GetApAgingReportQueryHandler/GetAllApInvoicesQueryHandler
/// use) so this dataset's figures always match those existing screens exactly — GetInvoiceTotal
/// branches on InvoiceType (ITEM invoices total `Lines`, EXPENSE invoices total `ExpenseLines`) and
/// GetAmountsPaidAsync sums only POSTED Outgoing Payment allocations. Both are computed in memory
/// after materializing/capping (same "bounded, occasional-use report" precedent as
/// GetInventoryAsOfQueryHandler), so they're display-only columns (Filterable/Sortable = false)
/// rather than pushed into the SQL predicate/order-by.
/// </summary>
public sealed class ApInvoicesReportDataset : IReportDataset
{
    public string Key => "AP_INVOICES";
    public string Label => "AP Invoices";
    public string RequiredPermissionCode => "AP_INVOICES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("InvoiceNo", "Invoice No.", ReportFieldType.Text),
        new("SupplierInvoiceNo", "Supplier Invoice No.", ReportFieldType.Text),
        new("InvoiceDate", "Invoice Date", ReportFieldType.Date),
        new("DueDate", "Due Date", ReportFieldType.Date),
        new("SupplierName", "Supplier", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
        new("OutstandingBalance", "Outstanding Balance", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ApInvoice> query = dbContext.ApInvoices.AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .Include(i => i.ExpenseLines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "InvoiceNo" => ReportDatasetFilters.Text(query, filter, i => i.InvoiceNo),
                "SupplierInvoiceNo" => ReportDatasetFilters.Text(query, filter, i => i.SupplierInvoiceNo),
                "InvoiceDate" => ReportDatasetFilters.Date(query, filter, i => (DateTimeOffset?)i.InvoiceDate),
                "DueDate" => ReportDatasetFilters.Date(query, filter, i => i.DueDate),
                "SupplierName" => ReportDatasetFilters.Text(query, filter, i => i.Supplier.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, i => i.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, i => i.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "InvoiceNo" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.InvoiceNo),
            "SupplierInvoiceNo" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.SupplierInvoiceNo),
            "InvoiceDate" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.InvoiceDate),
            "DueDate" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.DueDate),
            "SupplierName" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Supplier.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Status),
            _ => query.OrderByDescending(i => i.InvoiceDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var invoices = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var amountsPaid = await ApInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), cancellationToken);

        var rows = invoices.Select(i => BuildRow(i, request.ColumnKeys, amountsPaid.GetValueOrDefault(i.Id))).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(ApInvoice invoice, IReadOnlyList<string> columnKeys, decimal amountPaid)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "InvoiceNo" => invoice.InvoiceNo,
                "SupplierInvoiceNo" => invoice.SupplierInvoiceNo,
                "InvoiceDate" => invoice.InvoiceDate,
                "DueDate" => invoice.DueDate,
                "SupplierName" => invoice.Supplier.Name,
                "BranchId" => invoice.BranchId,
                "Status" => invoice.Status,
                "Amount" => Math.Round(ApInvoicePaymentBalance.GetInvoiceTotal(invoice), 2),
                "OutstandingBalance" => Math.Round(ApInvoicePaymentBalance.GetInvoiceTotal(invoice) - amountPaid, 2),
                _ => null,
            };
        }
        return row;
    }
}
