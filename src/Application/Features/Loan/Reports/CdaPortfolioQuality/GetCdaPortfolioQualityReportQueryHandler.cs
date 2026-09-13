namespace ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetCdaPortfolioQualityReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetCdaPortfolioQualityReportQuery, Result<CdaPortfolioQualityReportResponse>>
{
    public async Task<Result<CdaPortfolioQualityReportResponse>> HandleAsync(GetCdaPortfolioQualityReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<CdaPortfolioQualityReportResponse>(Error.Forbidden("CdaPortfolioQuality.Forbidden", "You do not have permission to view loan accounts."));

        var accountsQuery = dbContext.LoanAccounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.ScheduleLines)
            .Where(a => a.Status == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(query.BranchId)) accountsQuery = accountsQuery.Where(a => a.BranchId == query.BranchId);

        var accounts = await accountsQuery.ToListAsync(cancellationToken);
        if (accounts.Count == 0)
            return Result.Success(new CdaPortfolioQualityReportResponse([], 0, 0, 0, 0, 0, 0, 0, 0));

        var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;
        var accountIds = accounts.Select(a => a.Id).ToList();

        var lastLedgerByAccount = await dbContext.LoanLedgerEntries.AsNoTracking()
            .Where(l => accountIds.Contains(l.LoanAccountId))
            .GroupBy(l => l.LoanAccountId)
            .Select(g => g.OrderByDescending(l => l.SequenceNo).First())
            .ToDictionaryAsync(l => l.LoanAccountId, cancellationToken);

        var restructuredNewAccountIds = await dbContext.LoanRestructurings.AsNoTracking()
            .Where(r => r.Status == "POSTED" && r.NewLoanAccountId != null && accountIds.Contains(r.NewLoanAccountId.Value))
            .Select(r => r.NewLoanAccountId!.Value)
            .ToListAsync(cancellationToken);
        var restructuredSet = restructuredNewAccountIds.ToHashSet();

        var rows = accounts.Select(account =>
        {
            var oldestUnpaid = account.ScheduleLines
                .Where(l => l.Status != "PAID" && l.Status != "SUPERSEDED")
                .OrderBy(l => l.DueDate)
                .FirstOrDefault();
            var daysOverdue = oldestUnpaid is null || oldestUnpaid.DueDate > asOfDate
                ? 0
                : (int)Math.Floor((asOfDate - oldestUnpaid.DueDate).TotalDays);

            var balance = lastLedgerByAccount.TryGetValue(account.Id, out var lastLedger) ? lastLedger.RunningPrincipalBalance : 0m;

            var classification = ClassifyAccount(account, daysOverdue, restructuredSet.Contains(account.Id));

            // PAR = the entire outstanding balance once overdue at all (MC 02-04's own definition),
            // not just the overdue installment amount.
            var parAmount = daysOverdue > 0 ? balance : 0m;
            var apllPct = daysOverdue <= 0 ? 0m : daysOverdue <= 365 ? 0.35m : 1.00m;
            var apllAmount = parAmount * apllPct;

            return new CdaPortfolioQualityAccountRow(
                account.Id, account.LoanAcctNo, account.BranchId, account.CustomerId, account.Customer.Name, account.Customer.MemberNo,
                balance, daysOverdue, classification, parAmount, apllPct, apllAmount);
        })
        .OrderByDescending(r => r.ParAmount)
        .ToList();

        var totalOutstanding = rows.Sum(r => r.OutstandingPrincipalBalance);
        var totalPar = rows.Sum(r => r.ParAmount);

        var response = new CdaPortfolioQualityReportResponse(
            rows,
            totalOutstanding,
            totalPar,
            totalOutstanding > 0 ? totalPar / totalOutstanding : 0m,
            rows.Sum(r => r.ApllRequiredAmount),
            rows.Count(r => r.Classification == "CURRENT"),
            rows.Count(r => r.Classification == "PAST_DUE"),
            rows.Count(r => r.Classification == "IN_LITIGATION"),
            rows.Count(r => r.Classification == "RESTRUCTURED"));

        return Result.Success(response);
    }

    private static string ClassifyAccount(LoanAccount account, int daysOverdue, bool isRestructuredAccount)
    {
        // "In litigation" (a legal dispute) takes priority — MC 02-04 tracks it as its own
        // sub-account (153) regardless of how it would otherwise classify.
        if (account.IsDisputed) return "IN_LITIGATION";

        if (isRestructuredAccount && !RestructuredLoanGraduation.HasGraduated(account))
        {
            // "Past due" only once something has actually come due with zero payments against it —
            // a freshly restructured account whose first installment isn't due yet hasn't had a
            // chance to default, so it stays RESTRUCTURED rather than being flagged PAST_DUE on day one.
            return daysOverdue > 0 && !RestructuredLoanGraduation.HasAnyPayment(account) ? "PAST_DUE" : "RESTRUCTURED";
        }

        return daysOverdue > 0 ? "PAST_DUE" : "CURRENT";
    }
}
