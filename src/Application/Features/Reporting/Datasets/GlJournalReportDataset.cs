namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over GL Journals, modeled one row per journal (not per line) — a
/// journal's TotalDebit/TotalCredit summarize its Lines, computed in memory after materializing
/// (same "bounded, occasional-use report" precedent as GetInventoryAsOfQueryHandler) so they're
/// display-only columns (Filterable/Sortable = false). Deliberately includes REVERSED journals with
/// no extra Status filter, matching GetGeneralJournalReportQueryHandler's own documented reasoning —
/// a reversal is itself a real posted entry.
/// </summary>
public sealed class GlJournalReportDataset : IReportDataset
{
    public string Key => "GL_JOURNAL";
    public string Label => "GL Journal";
    public string RequiredPermissionCode => "GL_JOURNALS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("JournalNo", "Journal No.", ReportFieldType.Text),
        new("JournalDate", "Journal Date", ReportFieldType.Date),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Description", "Description", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("TotalDebit", "Total Debit", ReportFieldType.Currency, Filterable: false, Sortable: false),
        new("TotalCredit", "Total Credit", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GlJournal> query = dbContext.GlJournals.AsNoTracking()
            .Include(j => j.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "JournalNo" => ReportDatasetFilters.Text(query, filter, j => j.JournalNo),
                "JournalDate" => ReportDatasetFilters.Date(query, filter, j => (DateTimeOffset?)j.JournalDate),
                "BranchId" => ReportDatasetFilters.Text(query, filter, j => j.BranchId),
                "Description" => ReportDatasetFilters.Text(query, filter, j => j.Description),
                "Status" => ReportDatasetFilters.Text(query, filter, j => j.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "JournalNo" => ReportDatasetFilters.Sort(query, request.SortDescending, j => j.JournalNo),
            "JournalDate" => ReportDatasetFilters.Sort(query, request.SortDescending, j => j.JournalDate),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, j => j.BranchId),
            "Description" => ReportDatasetFilters.Sort(query, request.SortDescending, j => j.Description),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, j => j.Status),
            _ => query.OrderByDescending(j => j.JournalDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var journals = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = journals.Select(j => BuildRow(j, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(GlJournal journal, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "JournalNo" => journal.JournalNo,
                "JournalDate" => journal.JournalDate,
                "BranchId" => journal.BranchId,
                "Description" => journal.Description,
                "Status" => journal.Status,
                "TotalDebit" => Math.Round(journal.Lines.Sum(l => l.DebitAmount), 2),
                "TotalCredit" => Math.Round(journal.Lines.Sum(l => l.CreditAmount), 2),
                _ => null,
            };
        }
        return row;
    }
}
