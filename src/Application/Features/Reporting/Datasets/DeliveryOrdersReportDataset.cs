namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Delivery Orders — a generic data browser, not a BIR-compliance
/// report, mirroring SalesInvoicesReportDataset's own pattern. DeliveryOrder carries its Customer
/// as a direct FK/navigation (not only reachable via its optional SalesOrder link), so no Amount
/// field is proposed here: DeliveryOrderLine has no unit price, only UnitCost (the COGS cost snapshot,
/// not a sale value) and QtyShipped, so there's no natural sales "Amount" to surface — unlike
/// SalesOrder/SalesInvoice. The dataset's own field key "DeliveryNo" maps to the entity's actual
/// property name "DoNo" — field keys are this dataset's own arbitrary identifiers, not required to
/// match the CLR property name verbatim (see SalesOrdersReportDataset's own "OrderNo"/"SoNo" note).
/// </summary>
public sealed class DeliveryOrdersReportDataset : IReportDataset
{
    public string Key => "DELIVERY_ORDERS";
    public string Label => "Delivery Orders";
    public string RequiredPermissionCode => "DELIVERIES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("DeliveryNo", "Delivery No.", ReportFieldType.Text),
        new("DeliveryDate", "Delivery Date", ReportFieldType.Date),
        new("CustomerName", "Customer", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<DeliveryOrder> query = dbContext.DeliveryOrders.AsNoTracking()
            .Include(d => d.Customer);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "DeliveryNo" => ReportDatasetFilters.Text(query, filter, d => d.DoNo),
                "DeliveryDate" => ReportDatasetFilters.Date(query, filter, d => (DateTimeOffset?)d.DeliveryDate),
                "CustomerName" => ReportDatasetFilters.Text(query, filter, d => d.Customer.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, d => d.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, d => d.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "DeliveryNo" => ReportDatasetFilters.Sort(query, request.SortDescending, d => d.DoNo),
            "DeliveryDate" => ReportDatasetFilters.Sort(query, request.SortDescending, d => d.DeliveryDate),
            "CustomerName" => ReportDatasetFilters.Sort(query, request.SortDescending, d => d.Customer.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, d => d.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, d => d.Status),
            _ => query.OrderByDescending(d => d.DeliveryDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var deliveries = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = deliveries.Select(d => BuildRow(d, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(DeliveryOrder delivery, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "DeliveryNo" => delivery.DoNo,
                "DeliveryDate" => delivery.DeliveryDate,
                "CustomerName" => delivery.Customer.Name,
                "BranchId" => delivery.BranchId,
                "Status" => delivery.Status,
                _ => null,
            };
        }
        return row;
    }
}
