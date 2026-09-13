namespace ZARI.Application.Features.Loan.Reports.LoanDelinquency;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanDelinquencyReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLoanDelinquencyReportQuery, Result<LoanDelinquencyReportResponse>>
{
    public async Task<Result<LoanDelinquencyReportResponse>> HandleAsync(GetLoanDelinquencyReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<LoanDelinquencyReportResponse>(Error.Forbidden("LoanDelinquencyReport.Forbidden", "You do not have permission to view loan accounts."));

        var accountsQuery = dbContext.LoanAccounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.ScheduleLines)
            .Where(a => a.Status == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(query.BranchId)) accountsQuery = accountsQuery.Where(a => a.BranchId == query.BranchId);
        if (query.CustomerId is { } customerId) accountsQuery = accountsQuery.Where(a => a.CustomerId == customerId);

        var accounts = await accountsQuery.ToListAsync(cancellationToken);

        var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;

        var rows = accounts
            .SelectMany(account => account.ScheduleLines
                .Where(line => line.Status != "PAID")
                .Select(line =>
                {
                    var principalOutstanding = line.PrincipalDue - line.PrincipalPaid;
                    var interestOutstanding = line.InterestDue - line.InterestPaid;
                    var penaltyOutstanding = LoanPenaltyCalculator.ComputeOwed(line, asOfDate, account.PenaltyRatePct, account.GracePeriodDays);
                    var outstanding = principalOutstanding + interestOutstanding + penaltyOutstanding;
                    var daysOverdue = (int)Math.Floor((asOfDate - line.DueDate).TotalDays);
                    return new
                    {
                        Account = account,
                        Line = line,
                        PrincipalOutstanding = principalOutstanding,
                        InterestOutstanding = interestOutstanding,
                        PenaltyOutstanding = penaltyOutstanding,
                        Outstanding = outstanding,
                        DaysOverdue = daysOverdue,
                        Bucket = BucketOf(daysOverdue),
                    };
                }))
            // A line with nothing left owed (e.g. a PARTIALLY_PAID line whose remaining balance
            // rounds to zero) has nothing to age.
            .Where(x => x.Outstanding >= 0.01m)
            .OrderByDescending(x => x.DaysOverdue)
            .ToList();

        var groups = rows
            .GroupBy(x => x.Account.CustomerId)
            .Select(g =>
            {
                var first = g.First().Account;
                return new LoanDelinquencyCustomerGroup(
                    g.Key,
                    first.Customer.Name,
                    first.Customer.MemberNo,
                    g.Sum(x => x.Outstanding),
                    g.Select(x => new LoanDelinquencyInstallmentRow(
                        x.Account.Id,
                        x.Account.LoanAcctNo,
                        x.Account.BranchId,
                        x.Line.InstallmentNo,
                        x.Line.DueDate,
                        x.DaysOverdue,
                        x.Bucket,
                        x.PrincipalOutstanding,
                        x.InterestOutstanding,
                        x.PenaltyOutstanding,
                        x.Outstanding)).ToList());
            })
            .OrderByDescending(g => g.GroupTotal)
            .ToList();

        var response = new LoanDelinquencyReportResponse(
            groups,
            rows.Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "current").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "1-30").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "31-60").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "61-90").Sum(x => x.Outstanding),
            rows.Where(x => x.Bucket == "90+").Sum(x => x.Outstanding));

        return Result.Success(response);
    }

    private static string BucketOf(int daysOverdue) => daysOverdue switch
    {
        <= 0 => "current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };
}
