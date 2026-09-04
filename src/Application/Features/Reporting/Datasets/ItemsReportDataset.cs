namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Item master data — every field here is a plain scalar column or a
/// simple one-hop navigation join (Category.Name, BaseUom.Code), so unlike this module's
/// transactional datasets, nothing here needs an in-memory computed column: every field stays fully
/// filterable/sortable at the SQL level. Master data has no natural transaction date to default-sort
/// by, so falls back to CreatedAt descending (newest items first) when no SortFieldKey is given.
/// </summary>
public sealed class ItemsReportDataset : IReportDataset
{
    public string Key => "ITEMS";
    public string Label => "Items";
    public string RequiredPermissionCode => "ITEMS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("CategoryName", "Category", ReportFieldType.Text),
        new("UomCode", "UOM", ReportFieldType.Text),
        new("VatType", "VAT Type", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Item> query = dbContext.Items.AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.BaseUom);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, i => i.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, i => i.Name),
                "CategoryName" => ReportDatasetFilters.Text(query, filter, i => i.Category!.Name),
                "UomCode" => ReportDatasetFilters.Text(query, filter, i => i.BaseUom.Code),
                "VatType" => ReportDatasetFilters.Text(query, filter, i => i.VatType),
                "Status" => ReportDatasetFilters.Text(query, filter, i => i.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Name),
            "CategoryName" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Category!.Name),
            "UomCode" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.BaseUom.Code),
            "VatType" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.VatType),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, i => i.Status),
            _ => query.OrderByDescending(i => i.CreatedAt),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var items = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = items.Select(i => BuildRow(i, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Item item, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => item.Code,
                "Name" => item.Name,
                "CategoryName" => item.Category?.Name,
                "UomCode" => item.BaseUom.Code,
                "VatType" => item.VatType,
                "Status" => item.Status,
                _ => null,
            };
        }
        return row;
    }
}
