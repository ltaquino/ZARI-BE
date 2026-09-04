namespace ZARI.Application.Features.Accounting.Reports.GlAccountLedger;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// A single GL account's ledger card — opening balance, every journal line that hit it within the
/// chosen date range in chronological order, and a running balance. Every GlJournal row counts
/// regardless of its own Status — see GetTrialBalanceReportQueryHandler's note on why excluding
/// REVERSED rows would be wrong (it's a real historical posting too, not a draft). The opening
/// balance and running-balance walk are computed in memory rather than in SQL (same justification
/// as the precedent GetInventoryAsOfQueryHandler): a bounded, single-account row set from an
/// occasional-use reporting query, not a hot path.
/// </summary>
public sealed class GetGlAccountLedgerReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetGlAccountLedgerReportQuery, Result<GlAccountLedgerReportResponse>>
{
    public async Task<Result<GlAccountLedgerReportResponse>> HandleAsync(GetGlAccountLedgerReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_JOURNALS", FormAction.View, cancellationToken))
            return Result.Failure<GlAccountLedgerReportResponse>(Error.Forbidden("GlAccountLedgerReport.Forbidden", "You do not have permission to view GL journals."));

        var account = await dbContext.GlAccounts.AsNoTracking()
            .Where(a => a.Id == query.AccountId)
            .Select(a => new { a.Code, a.Name, a.NormalBalance })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result.Failure<GlAccountLedgerReportResponse>(Error.NotFound("GlAccountLedgerReport.NotFound", "GL account not found."));

        var flat = await dbContext.GlJournals.AsNoTracking()
            .Where(j => query.BranchId == null || j.BranchId == query.BranchId)
            .SelectMany(j => j.Lines.Where(l => l.AccountId == query.AccountId), (j, l) => new
            {
                JournalId = j.Id,
                j.JournalNo,
                j.JournalDate,
                j.BranchId,
                l.Memo,
                Debit = l.DebitAmount,
                Credit = l.CreditAmount,
            })
            .ToListAsync(cancellationToken);

        var ordered = flat
            .OrderBy(f => f.JournalDate)
            .ThenBy(f => f.JournalNo, StringComparer.Ordinal)
            .ToList();

        var fromMs = query.FromDate;
        var toMs = query.ToDate.HasValue
            ? new DateTimeOffset(query.ToDate.Value.Date, query.ToDate.Value.Offset).AddDays(1).AddTicks(-1)
            : (DateTimeOffset?)null;

        var sign = account.NormalBalance == "Debit" ? 1m : -1m;

        var opening = 0m;
        if (fromMs.HasValue)
        {
            foreach (var f in ordered)
            {
                if (f.JournalDate < fromMs.Value)
                    opening += sign * (f.Debit - f.Credit);
            }
        }

        var running = opening;
        decimal periodDebit = 0, periodCredit = 0;
        var lines = new List<GlAccountLedgerLine>();
        foreach (var f in ordered)
        {
            if (fromMs.HasValue && f.JournalDate < fromMs.Value) continue;
            if (toMs.HasValue && f.JournalDate > toMs.Value) continue;

            running += sign * (f.Debit - f.Credit);
            periodDebit += f.Debit;
            periodCredit += f.Credit;
            lines.Add(new GlAccountLedgerLine(f.JournalId, f.JournalNo, f.JournalDate, f.BranchId, f.Memo, f.Debit, f.Credit, running));
        }

        return Result.Success(new GlAccountLedgerReportResponse(account.Code, account.Name, opening, running, periodDebit, periodCredit, lines));
    }
}
