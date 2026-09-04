namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Warehouse master data — every field is a plain scalar column, so
/// nothing here needs an in-memory computed column: every field stays fully filterable/sortable at
/// the SQL level. Small master-data table, so — unlike the transactional datasets — defaults to
/// Name ascending rather than a date field when no SortFieldKey is given.
/// </summary>
public sealed class WarehousesReportDataset : IReportDataset
{
    public string Key => "WAREHOUSES";
    public string Label => "Warehouses";
    public string RequiredPermissionCode => "WAREHOUSES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Warehouse> query = dbContext.Warehouses.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, w => w.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, w => w.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, w => w.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, w => w.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, w => w.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, w => w.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, w => w.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, w => w.Status),
            _ => query.OrderBy(w => w.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var warehouses = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = warehouses.Select(w => BuildRow(w, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Warehouse warehouse, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => warehouse.Code,
                "Name" => warehouse.Name,
                "BranchId" => warehouse.BranchId,
                "Status" => warehouse.Status,
                _ => null,
            };
        }
        return row;
    }
}
