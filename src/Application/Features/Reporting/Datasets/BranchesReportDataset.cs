namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Branches — small master data, no .Include()s needed. Branch has its
/// own Code property distinct from Id (Id is the internal FK slug, e.g. "br-hq"; Code is a separate
/// display code) — Code is used as the primary identifying field, matching every other dataset's
/// convention. Default sort is Code ascending.
///
/// BranchId (backed by the entity's own Id) is also exposed here — unlike every other dataset,
/// where BranchId is a foreign key to a DIFFERENT branch-scoped entity, here it IS the row's own
/// identity. It exists specifically so ReportBranchScope's generic "every dataset with a BranchId
/// field gets scoped to the current user's UserBranch assignments" mechanism also covers the
/// Branches listing itself, with no special-casing needed in the engine for this one dataset.
/// </summary>
public sealed class BranchesReportDataset : IReportDataset
{
    public string Key => "BRANCHES";
    public string Label => "Branches";
    public string RequiredPermissionCode => "BRANCHES";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("BranchId", "Branch ID", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Branch> query = dbContext.Branches.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, b => b.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, b => b.Name),
                "Status" => ReportDatasetFilters.Text(query, filter, b => b.Status),
                "BranchId" => ReportDatasetFilters.Text(query, filter, b => b.Id),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Name),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Status),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Id),
            _ => query.OrderBy(b => b.Code),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var branches = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = branches.Select(b => BuildRow(b, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Branch branch, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => branch.Code,
                "Name" => branch.Name,
                "Status" => branch.Status,
                "BranchId" => branch.Id,
                _ => null,
            };
        }
        return row;
    }
}
