namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetGovernmentContributionRateQuery(Guid Id) : IQuery<Result<GovernmentContributionRateResponse>>;

public sealed record GovernmentContributionRateResponse(
    Guid Id,
    string ContributionType,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeSharePct,
    decimal EmployerSharePct,
    decimal? MaxContribution,
    DateTimeOffset EffectiveDate,
    string Status,
    DateTimeOffset CreatedAt);
