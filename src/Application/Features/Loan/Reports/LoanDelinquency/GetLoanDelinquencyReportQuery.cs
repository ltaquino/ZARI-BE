namespace ZARI.Application.Features.Loan.Reports.LoanDelinquency;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Outstanding installments on every ACTIVE loan account, grouped by member and how overdue they
/// are against each installment's own due date — the AP Aging report's bucketing pattern
/// (Application/Features/Purchasing/Reports/ApAging/) applied to LoanAmortizationScheduleLine
/// instead of AP invoices. AsOfDate defaults to today (server clock) when omitted.
/// </summary>
public sealed record GetLoanDelinquencyReportQuery(string? BranchId, Guid? CustomerId, DateTimeOffset? AsOfDate) : IQuery<Result<LoanDelinquencyReportResponse>>;

public sealed record LoanDelinquencyInstallmentRow(
    Guid LoanAccountId,
    string LoanAcctNo,
    string BranchId,
    int InstallmentNo,
    DateTimeOffset DueDate,
    int DaysOverdue,
    string Bucket,
    decimal PrincipalOutstanding,
    decimal InterestOutstanding,
    decimal PenaltyOutstanding,
    decimal Outstanding);

public sealed record LoanDelinquencyCustomerGroup(
    Guid CustomerId,
    string CustomerName,
    string? MemberNo,
    decimal GroupTotal,
    List<LoanDelinquencyInstallmentRow> Installments);

public sealed record LoanDelinquencyReportResponse(
    List<LoanDelinquencyCustomerGroup> Groups,
    decimal TotalOutstanding,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus);
