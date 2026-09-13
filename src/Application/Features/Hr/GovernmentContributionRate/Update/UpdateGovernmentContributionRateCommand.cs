namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateGovernmentContributionRateCommand(
    Guid Id,
    string ContributionType,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeSharePct,
    decimal EmployerSharePct,
    decimal? MaxContribution,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand;
