namespace ZARI.Application.Features.Loan.LoanRestructurings.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateLoanRestructuringCommand(
    Guid Id,
    string BranchId,
    DateTimeOffset RestructureDate,
    decimal NewAnnualInterestRatePct,
    int NewTermMonths,
    string NewRepaymentFrequency,
    int NewGracePeriodDays,
    decimal NewPenaltyRatePct,
    DateTimeOffset NewFirstDueDate,
    string Reason,
    string? Remarks,
    string? UpdatedBy) : ICommand<Result<LoanRestructuringResponse>>;
