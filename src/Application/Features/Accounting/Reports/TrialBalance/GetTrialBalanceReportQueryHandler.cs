namespace ZARI.Application.Features.Accounting.Reports.TrialBalance;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// One row per GL account, each account's net activity (every journal line up to the chosen "as of"
/// date) presented in whichever column — Debit or Credit — matches its own normal balance. Every
/// GlJournal row counts here regardless of its own Status: "POSTED" and "REVERSED" both mean the
/// journal's lines actually hit the ledger at some point — REVERSED just means a later journal has
/// since counter-posted the opposite entries, which is itself its own "POSTED" row. Excluding
/// REVERSED rows here would double-subtract every cancelled document's effect instead of netting it
/// back to zero (mirrors the FE's TrialBalanceReportPage.tsx, ported verbatim).
/// </summary>
public sealed class GetTrialBalanceReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetTrialBalanceReportQuery, Result<TrialBalanceReportResponse>>
{
    public async Task<Result<TrialBalanceReportResponse>> HandleAsync(GetTrialBalanceReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_JOURNALS", FormAction.View, cancellationToken))
            return Result.Failure<TrialBalanceReportResponse>(Error.Forbidden("TrialBalanceReport.Forbidden", "You do not have permission to view GL journals."));

        // End of the chosen day, same tolerance the FE's `${asOfDate}T23:59:59.999` cutoff used.
        var cutoff = new DateTimeOffset(query.AsOfDate.Date, query.AsOfDate.Offset).AddDays(1).AddTicks(-1);

        var lineTotals = await dbContext.GlJournals.AsNoTracking()
            .Where(j => (query.BranchId == null || j.BranchId == query.BranchId) && j.JournalDate <= cutoff)
            .SelectMany(j => j.Lines)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, x => (x.Debit, x.Credit), cancellationToken);

        var accounts = await dbContext.GlAccounts.AsNoTracking()
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType, a.NormalBalance })
            .ToListAsync(cancellationToken);

        var allRows = accounts
            .Select(a =>
            {
                var (debit, credit) = lineTotals.GetValueOrDefault(a.Id, (0m, 0m));
                var net = a.NormalBalance == "Debit" ? debit - credit : credit - debit;
                var debitBalance = a.NormalBalance == "Debit" ? Math.Max(net, 0) : Math.Max(-net, 0);
                var creditBalance = a.NormalBalance == "Credit" ? Math.Max(net, 0) : Math.Max(-net, 0);
                return new TrialBalanceRow(a.Id, a.Code, a.Name, a.AccountType, debitBalance, creditBalance);
            })
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .ToList();

        var rows = query.IncludeZeroBalances
            ? allRows
            : allRows.Where(r => r.DebitBalance != 0 || r.CreditBalance != 0).ToList();

        var totalDebit = rows.Sum(r => r.DebitBalance);
        var totalCredit = rows.Sum(r => r.CreditBalance);
        var isBalanced = Math.Abs(totalDebit - totalCredit) < 0.005m;

        return Result.Success(new TrialBalanceReportResponse(rows, totalDebit, totalCredit, isBalanced));
    }
}
