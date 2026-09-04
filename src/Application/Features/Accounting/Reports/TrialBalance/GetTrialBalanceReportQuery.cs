namespace ZARI.Application.Features.Accounting.Reports.TrialBalance;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetTrialBalanceReportQuery(string? BranchId, DateTimeOffset AsOfDate, bool IncludeZeroBalances = false)
    : IQuery<Result<TrialBalanceReportResponse>>;

public sealed record TrialBalanceRow(Guid AccountId, string Code, string Name, string AccountType, decimal DebitBalance, decimal CreditBalance);

public sealed record TrialBalanceReportResponse(List<TrialBalanceRow> Rows, decimal TotalDebit, decimal TotalCredit, bool IsBalanced);
