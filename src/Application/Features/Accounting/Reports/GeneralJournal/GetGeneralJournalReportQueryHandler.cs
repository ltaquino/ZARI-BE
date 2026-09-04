namespace ZARI.Application.Features.Accounting.Reports.GeneralJournal;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// The BIR General Journal book — every GL journal's lines, in chronological (posting-date) order,
/// grouped visually by journal voucher. REVERSED journals are included (that reversal is itself its
/// own real posted entry, its Status surfaced so the FE can still badge it) — same reasoning
/// GetTrialBalanceReportQueryHandler documents. Ported from GeneralJournalReportPage.tsx.
/// </summary>
public sealed class GetGeneralJournalReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetGeneralJournalReportQuery, Result<GeneralJournalReportResponse>>
{
    public async Task<Result<GeneralJournalReportResponse>> HandleAsync(GetGeneralJournalReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_JOURNALS", FormAction.View, cancellationToken))
            return Result.Failure<GeneralJournalReportResponse>(Error.Forbidden("GeneralJournalReport.Forbidden", "You do not have permission to view GL journals."));

        var fromCutoff = query.FromDate.HasValue
            ? new DateTimeOffset(query.FromDate.Value.Date, query.FromDate.Value.Offset)
            : (DateTimeOffset?)null;
        var toCutoff = query.ToDate.HasValue
            ? new DateTimeOffset(query.ToDate.Value.Date, query.ToDate.Value.Offset).AddDays(1).AddTicks(-1)
            : (DateTimeOffset?)null;

        var journals = await dbContext.GlJournals.AsNoTracking()
            .Include(j => j.Lines)
            .Where(j => query.BranchId == null || j.BranchId == query.BranchId)
            .Where(j => !fromCutoff.HasValue || j.JournalDate >= fromCutoff.Value)
            .Where(j => !toCutoff.HasValue || j.JournalDate <= toCutoff.Value)
            .OrderBy(j => j.JournalDate)
            .ToListAsync(cancellationToken);

        var accountNames = await dbContext.GlAccounts.AsNoTracking()
            .Select(a => new { a.Id, a.Name })
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var entries = journals
            .Select(j => new GeneralJournalEntry(
                j.Id,
                j.JournalNo,
                j.JournalDate,
                j.Description,
                j.BranchId,
                j.Status,
                j.Lines
                    .Select(l => new GeneralJournalLine(l.AccountId, accountNames.GetValueOrDefault(l.AccountId, "Unknown"), l.Memo, l.DebitAmount, l.CreditAmount))
                    .ToList()))
            .ToList();

        var totalDebit = entries.Sum(e => e.Lines.Sum(l => l.Debit));
        var totalCredit = entries.Sum(e => e.Lines.Sum(l => l.Credit));

        return Result.Success(new GeneralJournalReportResponse(entries, totalDebit, totalCredit));
    }
}
