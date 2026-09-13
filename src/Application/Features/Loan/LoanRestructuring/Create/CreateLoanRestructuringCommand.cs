namespace ZARI.Application.Features.Loan.LoanRestructurings.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record CreateLoanRestructuringCommand(
    string BranchId,
    Guid OldLoanAccountId,
    DateTimeOffset RestructureDate,
    decimal NewAnnualInterestRatePct,
    int NewTermMonths,
    string NewRepaymentFrequency,
    int NewGracePeriodDays,
    decimal NewPenaltyRatePct,
    DateTimeOffset NewFirstDueDate,
    string Reason,
    string? Remarks,
    string? CreatedBy) : ICommand<Result<LoanRestructuringResponse>>;
