namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Domain.Common;

public sealed record CreateGovernmentContributionRateCommand(
    string ContributionType,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeSharePct,
    decimal EmployerSharePct,
    decimal? MaxContribution,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand<Result<GovernmentContributionRateResponse>>;
