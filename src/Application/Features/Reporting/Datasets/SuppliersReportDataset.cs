namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Supplier master data — every field here is a plain scalar column
/// (no navigation joins needed), so unlike this module's transactional datasets, nothing here
/// needs an in-memory computed column: every field stays fully filterable/sortable at the SQL
/// level. PaymentTermsDays is a nullable int, filtered/sorted via a harmless `(decimal?)s.PaymentTermsDays`
/// cast at the call site (same pattern the other datasets use to adapt a non-nullable numeric
/// property to ReportDatasetFilters.Decimal's `decimal?` selector). Master data has no natural
/// transaction date to default-sort by, so falls back to Name ascending when no SortFieldKey is given.
/// </summary>
public sealed class SuppliersReportDataset : IReportDataset
{
    public string Key => "SUPPLIERS";
    public string Label => "Suppliers";
    public string RequiredPermissionCode => "SUPPLIERS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("ContactPerson", "Contact Person", ReportFieldType.Text),
        new("ContactNumber", "Phone", ReportFieldType.Text),
        new("PaymentTermsDays", "Payment Terms (Days)", ReportFieldType.Number),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Supplier> query = dbContext.Suppliers.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, s => s.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, s => s.Name),
                "ContactPerson" => ReportDatasetFilters.Text(query, filter, s => s.ContactPerson),
                "ContactNumber" => ReportDatasetFilters.Text(query, filter, s => s.ContactNumber),
                "PaymentTermsDays" => ReportDatasetFilters.Decimal(query, filter, s => (decimal?)s.PaymentTermsDays),
                "Status" => ReportDatasetFilters.Text(query, filter, s => s.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.Name),
            "ContactPerson" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.ContactPerson),
            "ContactNumber" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.ContactNumber),
            "PaymentTermsDays" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.PaymentTermsDays),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, s => s.Status),
            _ => query.OrderBy(s => s.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var suppliers = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = suppliers.Select(s => BuildRow(s, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Supplier supplier, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => supplier.Code,
                "Name" => supplier.Name,
                "ContactPerson" => supplier.ContactPerson,
                "ContactNumber" => supplier.ContactNumber,
                "PaymentTermsDays" => supplier.PaymentTermsDays,
                "Status" => supplier.Status,
                _ => null,
            };
        }
        return row;
    }
}
