namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Purchase Orders. PurchaseOrder has no TotalAmount-equivalent
/// property (same situation GetCashOutRegisterReportQueryHandler already documents for
/// OutgoingPayment), so Amount is the sum of each line's Qty x UnitCost, computed in memory after
/// materializing/capping (same "bounded, occasional-use report" precedent as
/// GetInventoryAsOfQueryHandler / ApInvoicesReportDataset) — a display-only column
/// (Filterable/Sortable = false) rather than pushed into the SQL predicate/order-by.
/// </summary>
public sealed class PurchaseOrdersReportDataset : IReportDataset
{
    public string Key => "PURCHASE_ORDERS";
    public string Label => "Purchase Orders";
    public string RequiredPermissionCode => "PURCHASE_ORDERS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("PoNo", "PO No.", ReportFieldType.Text),
        new("OrderDate", "Order Date", ReportFieldType.Date),
        new("SupplierName", "Supplier", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<PurchaseOrder> query = dbContext.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "PoNo" => ReportDatasetFilters.Text(query, filter, p => p.PoNo),
                "OrderDate" => ReportDatasetFilters.Date(query, filter, p => (DateTimeOffset?)p.OrderDate),
                "SupplierName" => ReportDatasetFilters.Text(query, filter, p => p.Supplier.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, p => p.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, p => p.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "PoNo" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.PoNo),
            "OrderDate" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.OrderDate),
            "SupplierName" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Supplier.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Status),
            _ => query.OrderByDescending(p => p.OrderDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var orders = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = orders.Select(p => BuildRow(p, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(PurchaseOrder order, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "PoNo" => order.PoNo,
                "OrderDate" => order.OrderDate,
                "SupplierName" => order.Supplier.Name,
                "BranchId" => order.BranchId,
                "Status" => order.Status,
                "Amount" => Math.Round(order.Lines.Sum(l => l.Qty * l.UnitCost), 2),
                _ => null,
            };
        }
        return row;
    }
}
