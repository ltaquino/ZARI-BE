namespace ZARI.Application.Features.Hr.SssContributionBrackets.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateSssContributionBracketCommand(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand;
