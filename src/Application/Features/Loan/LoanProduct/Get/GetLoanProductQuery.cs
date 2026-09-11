namespace ZARI.Application.Features.Loan.LoanProducts.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetLoanProductQuery(Guid Id) : IQuery<Result<LoanProductResponse>>;

public sealed record LoanProductResponse(
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
    string Status,
    DateTimeOffset CreatedAt);
