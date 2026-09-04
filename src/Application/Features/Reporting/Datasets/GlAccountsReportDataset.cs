namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over the GL Account chart of accounts — small master data, no
/// .Include()s needed. NormalBalance is its own first-class field on GlAccount (not derived), same
/// as AccountType, so both are plain Text filters/columns. Default sort is Code ascending, matching
/// how the chart of accounts is presented everywhere else in the app.
/// </summary>
public sealed class GlAccountsReportDataset : IReportDataset
{
    public string Key => "GL_ACCOUNTS";
    public string Label => "GL Accounts";
    public string RequiredPermissionCode => "GL_ACCOUNTS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("AccountType", "Account Type", ReportFieldType.Text),
        new("NormalBalance", "Normal Balance", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GlAccount> query = dbContext.GlAccounts.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, a => a.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, a => a.Name),
                "AccountType" => ReportDatasetFilters.Text(query, filter, a => a.AccountType),
                "NormalBalance" => ReportDatasetFilters.Text(query, filter, a => a.NormalBalance),
                "Status" => ReportDatasetFilters.Text(query, filter, a => a.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, a => a.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, a => a.Name),
            "AccountType" => ReportDatasetFilters.Sort(query, request.SortDescending, a => a.AccountType),
            "NormalBalance" => ReportDatasetFilters.Sort(query, request.SortDescending, a => a.NormalBalance),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, a => a.Status),
            _ => query.OrderBy(a => a.Code),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var accounts = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = accounts.Select(a => BuildRow(a, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(GlAccount account, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => account.Code,
                "Name" => account.Name,
                "AccountType" => account.AccountType,
                "NormalBalance" => account.NormalBalance,
                "Status" => account.Status,
                _ => null,
            };
        }
        return row;
    }
}
