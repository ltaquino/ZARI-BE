namespace ZARI.Application.Features.Loan.LoanRestructurings.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanRestructuringsQuery : IQuery<Result<List<LoanRestructuringResponse>>>;

public sealed record LoanRestructuringResponse(
    Guid Id,
    string RestructuringNo,
    string BranchId,
    Guid OldLoanAccountId,
    string OldLoanAcctNo,
    string CustomerName,
    string LoanProductName,
    Guid? NewLoanAccountId,
    string? NewLoanAcctNo,
    DateTimeOffset RestructureDate,
    decimal OldPrincipalBalance,
    decimal NewAnnualInterestRatePct,
    int NewTermMonths,
    string NewRepaymentFrequency,
    int NewGracePeriodDays,
    decimal NewPenaltyRatePct,
    DateTimeOffset NewFirstDueDate,
    string Reason,
    string Status,
    string? Remarks,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
