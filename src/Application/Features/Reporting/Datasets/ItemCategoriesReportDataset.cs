namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over ItemCategory master data — every field is a plain scalar column,
/// so nothing here needs an in-memory computed column: every field stays fully filterable/sortable
/// at the SQL level. ItemCategory has no Status property (unlike Warehouse/Item), so this dataset
/// exposes only its two actual scalar columns (its other property, ParentCategoryId, is a
/// self-referencing FK rather than a reportable scalar/label, so it's left out here same as this
/// module's other master-data datasets only surface plain columns and simple one-hop label joins).
/// Small master-data table, so defaults to Name ascending rather than a date field when no
/// SortFieldKey is given.
/// </summary>
public sealed class ItemCategoriesReportDataset : IReportDataset
{
    public string Key => "ITEM_CATEGORIES";
    public string Label => "Item Categories";
    public string RequiredPermissionCode => "ITEM_CATEGORIES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ItemCategory> query = dbContext.ItemCategories.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, c => c.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, c => c.Name),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Name),
            _ => query.OrderBy(c => c.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var categories = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = categories.Select(c => BuildRow(c, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(ItemCategory category, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => category.Code,
                "Name" => category.Name,
                _ => null,
            };
        }
        return row;
    }
}
