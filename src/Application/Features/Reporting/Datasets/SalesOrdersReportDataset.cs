namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Sales Orders — a generic data browser, not a BIR-compliance report,
/// mirroring SalesInvoicesReportDataset's own pattern. SalesOrder has no stored header total (unlike
/// SalesInvoice's PaidAmount-adjacent fields), so Amount is computed in memory after materializing:
/// the sum of each line's net total (Qty x UnitPrice x (1 - DiscountPct/100)), further reduced by
/// the order's own header DiscountPct (per SalesOrder's own doc comment: "applied after line-level
/// discounts"). The dataset's own field key "OrderNo" maps to the entity's actual property name
/// "SoNo" — field keys are this dataset's own arbitrary identifiers (see e.g. SalesInvoicesReportDataset's
/// "CustomerName" mapping to the Customer.Name navigation), not required to match the CLR property
/// name verbatim.
/// </summary>
public sealed class SalesOrdersReportDataset : IReportDataset
{
    public string Key => "SALES_ORDERS";
    public string Label => "Sales Orders";
    public string RequiredPermissionCode => "SALES_ORDERS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("OrderNo", "Order No.", ReportFieldType.Text),
        new("OrderDate", "Order Date", ReportFieldType.Date),
        new("CustomerName", "Customer", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = dbContext.SalesOrders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "OrderNo" => ReportDatasetFilters.Text(query, filter, o => o.SoNo),
                "OrderDate" => ReportDatasetFilters.Date(query, filter, o => (DateTimeOffset?)o.OrderDate),
                "CustomerName" => ReportDatasetFilters.Text(query, filter, o => o.Customer.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, o => o.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, o => o.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "OrderNo" => ReportDatasetFilters.Sort(query, request.SortDescending, o => o.SoNo),
            "OrderDate" => ReportDatasetFilters.Sort(query, request.SortDescending, o => o.OrderDate),
            "CustomerName" => ReportDatasetFilters.Sort(query, request.SortDescending, o => o.Customer.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, o => o.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, o => o.Status),
            _ => query.OrderByDescending(o => o.OrderDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var orders = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = orders.Select(o => BuildRow(o, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(SalesOrder order, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "OrderNo" => order.SoNo,
                "OrderDate" => order.OrderDate,
                "CustomerName" => order.Customer.Name,
                "BranchId" => order.BranchId,
                "Status" => order.Status,
                "Amount" => ComputeAmount(order),
                _ => null,
            };
        }
        return row;
    }

    private static decimal ComputeAmount(SalesOrder order)
    {
        var lineTotal = order.Lines.Sum(l => l.Qty * l.UnitPrice * (1 - l.DiscountPct / 100m));
        var headerDiscountPct = order.DiscountPct ?? 0m;
        return Math.Round(lineTotal * (1 - headerDiscountPct / 100m), 2);
    }
}
