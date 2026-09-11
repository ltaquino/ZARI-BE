namespace ZARI.Application.Features.Loan.LoanAccounts.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanAccountsQuery : IQuery<Result<List<LoanAccountResponse>>>;

public sealed record LoanAmortizationScheduleLineResponse(
    Guid Id,
    int InstallmentNo,
    DateTimeOffset DueDate,
    decimal PrincipalDue,
    decimal InterestDue,
    decimal TotalDue,
    decimal OutstandingPrincipalAfter,
    decimal PrincipalPaid,
    decimal InterestPaid,
    string Status);

public sealed record LoanAccountResponse(
    Guid Id,
    string LoanAcctNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    Guid LoanProductId,
    string LoanProductCode,
    string LoanProductName,
    Guid? LoanApplicationId,
    string? LoanApplicationNo,
    decimal PrincipalAmount,
    decimal AnnualInterestRatePct,
    int TermMonths,
    string RepaymentFrequency,
    int GracePeriodDays,
    decimal PenaltyRatePct,
    DateTimeOffset GrantDate,
    DateTimeOffset FirstDueDate,
    string Status,
    string? Remarks,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    List<LoanAmortizationScheduleLineResponse> ScheduleLines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
