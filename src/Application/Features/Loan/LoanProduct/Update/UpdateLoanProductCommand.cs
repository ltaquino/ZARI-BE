namespace ZARI.Application.Features.Loan.LoanProducts.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateLoanProductCommand(
    Guid Id,
    string Code,
    string Name,
    string InterestMethod,
    decimal AnnualInterestRatePct,
    decimal MinPrincipal,
    decimal MaxPrincipal,
    int MinTermMonths,
    int MaxTermMonths,
    string RepaymentFrequency,
    int GracePeriodDays,
    decimal PenaltyRatePct,
    bool RequiresCollateral,
    bool RequiresCoMaker,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    string Status) : ICommand;
