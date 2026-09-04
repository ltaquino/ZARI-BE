namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Purchase Requests. Every field here is a plain scalar column, so
/// (unlike PurchaseOrdersReportDataset/GoodsReceiptPoReportDataset/OutgoingPaymentsReportDataset,
/// none of which have a stored total) nothing here needs an in-memory computed column — every
/// field stays fully filterable/sortable at the SQL level.
/// </summary>
public sealed class PurchaseRequestsReportDataset : IReportDataset
{
    public string Key => "PURCHASE_REQUESTS";
    public string Label => "Purchase Requests";
    public string RequiredPermissionCode => "PURCHASE_REQUESTS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("RequestNo", "Request No.", ReportFieldType.Text),
        new("RequestDate", "Request Date", ReportFieldType.Date),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Remarks", "Remarks", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<PurchaseRequest> query = dbContext.PurchaseRequests.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "RequestNo" => ReportDatasetFilters.Text(query, filter, r => r.RequestNo),
                "RequestDate" => ReportDatasetFilters.Date(query, filter, r => (DateTimeOffset?)r.RequestDate),
                "BranchId" => ReportDatasetFilters.Text(query, filter, r => r.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, r => r.Status),
                "Remarks" => ReportDatasetFilters.Text(query, filter, r => r.Remarks),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "RequestNo" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.RequestNo),
            "RequestDate" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.RequestDate),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Status),
            "Remarks" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Remarks),
            _ => query.OrderByDescending(r => r.RequestDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var requests = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = requests.Select(r => BuildRow(r, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(PurchaseRequest requestEntity, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "RequestNo" => requestEntity.RequestNo,
                "RequestDate" => requestEntity.RequestDate,
                "BranchId" => requestEntity.BranchId,
                "Status" => requestEntity.Status,
                "Remarks" => requestEntity.Remarks,
                _ => null,
            };
        }
        return row;
    }
}
