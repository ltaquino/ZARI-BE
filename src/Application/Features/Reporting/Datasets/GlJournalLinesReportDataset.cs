namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;

/// <summary>
/// Report Designer dataset over GL Journal Lines — a line-level drill-down companion to
/// GlJournalReportDataset, for reports that need one row per debit/credit line rather than one row
/// per journal. GlJournalLine has no DbSet of its own (it's only ever read together with its parent
/// GlJournal), so the base query starts from GlJournals and flattens via SelectMany into this
/// dataset's own flat JournalLineRow projection — a compile-time-known shape so
/// ReportDatasetFilters' expression-tree helpers can still push every filter/sort down into SQL
/// exactly like the entity-based datasets do. Deliberately includes REVERSED journals with no extra
/// Status filter, matching GetGeneralJournalReportQueryHandler's own documented reasoning — a
/// reversal is itself a real posted entry — and Memo is read off the line (not the journal header),
/// same as that handler's GeneralJournalLine projection.
/// </summary>
public sealed class GlJournalLinesReportDataset : IReportDataset
{
    public string Key => "GL_JOURNAL_LINES";
    public string Label => "GL Journal Lines";
    public string RequiredPermissionCode => "GL_JOURNALS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("JournalNo", "Journal No.", ReportFieldType.Text),
        new("JournalDate", "Journal Date", ReportFieldType.Date),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("AccountName", "Account", ReportFieldType.Text),
        new("Debit", "Debit", ReportFieldType.Currency),
        new("Credit", "Credit", ReportFieldType.Currency),
        new("Memo", "Memo", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<JournalLineRow> query = dbContext.GlJournals.AsNoTracking()
            .Include(j => j.Lines).ThenInclude(l => l.Account)
            .SelectMany(j => j.Lines, (j, l) => new JournalLineRow(
                j.JournalNo, j.JournalDate, j.BranchId, l.Account.Name, l.DebitAmount, l.CreditAmount, l.Memo));

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "JournalNo" => ReportDatasetFilters.Text(query, filter, r => r.JournalNo),
                "JournalDate" => ReportDatasetFilters.Date(query, filter, r => (DateTimeOffset?)r.JournalDate),
                "BranchId" => ReportDatasetFilters.Text(query, filter, r => r.BranchId),
                "AccountName" => ReportDatasetFilters.Text(query, filter, r => r.AccountName),
                "Debit" => ReportDatasetFilters.Decimal(query, filter, r => (decimal?)r.Debit),
                "Credit" => ReportDatasetFilters.Decimal(query, filter, r => (decimal?)r.Credit),
                "Memo" => ReportDatasetFilters.Text(query, filter, r => r.Memo),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "JournalNo" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.JournalNo),
            "JournalDate" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.JournalDate),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.BranchId),
            "AccountName" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.AccountName),
            "Debit" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Debit),
            "Credit" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Credit),
            "Memo" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Memo),
            _ => query.OrderByDescending(r => r.JournalDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var rowsData = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = rowsData.Select(r => BuildRow(r, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(JournalLineRow row, IReadOnlyList<string> columnKeys)
    {
        var result = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            result[key] = key switch
            {
                "JournalNo" => row.JournalNo,
                "JournalDate" => row.JournalDate,
                "BranchId" => row.BranchId,
                "AccountName" => row.AccountName,
                "Debit" => row.Debit,
                "Credit" => row.Credit,
                "Memo" => row.Memo,
                _ => null,
            };
        }
        return result;
    }

    private sealed record JournalLineRow(
        string JournalNo,
        DateTimeOffset JournalDate,
        string BranchId,
        string AccountName,
        decimal Debit,
        decimal Credit,
        string? Memo);
}
