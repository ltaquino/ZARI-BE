namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Uom master data — every field is a plain scalar column, so nothing
/// here needs an in-memory computed column: every field stays fully filterable/sortable at the SQL
/// level. Uom has no Status property (unlike Warehouse/Item), so this dataset exposes only its two
/// actual columns. Small master-data table, so defaults to Name ascending rather than a date field
/// when no SortFieldKey is given.
/// </summary>
public sealed class UomsReportDataset : IReportDataset
{
    public string Key => "UOMS";
    public string Label => "Units of Measure";
    public string RequiredPermissionCode => "UOMS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Uom> query = dbContext.Uoms.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, u => u.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, u => u.Name),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, u => u.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, u => u.Name),
            _ => query.OrderBy(u => u.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var uoms = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = uoms.Select(u => BuildRow(u, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Uom uom, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => uom.Code,
                "Name" => uom.Name,
                _ => null,
            };
        }
        return row;
    }
}
