namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Cost Centers — small master data, no .Include()s needed.
/// BranchId is nullable on the entity itself (null means company-level, per CostCenter's own doc
/// comment), so its filter/sort use CostCenter's raw string? property directly. Default sort is
/// Code ascending.
/// </summary>
public sealed class CostCentersReportDataset : IReportDataset
{
    public string Key => "COST_CENTERS";
    public string Label => "Cost Centers";
    public string RequiredPermissionCode => "COST_CENTERS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<CostCenter> query = dbContext.CostCenters.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, c => c.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, c => c.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, c => c.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, c => c.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Status),
            _ => query.OrderBy(c => c.Code),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var costCenters = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = costCenters.Select(c => BuildRow(c, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(CostCenter costCenter, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => costCenter.Code,
                "Name" => costCenter.Name,
                "BranchId" => costCenter.BranchId,
                "Status" => costCenter.Status,
                _ => null,
            };
        }
        return row;
    }
}
