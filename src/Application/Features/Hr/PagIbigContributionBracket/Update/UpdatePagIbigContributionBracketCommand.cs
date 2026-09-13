namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePagIbigContributionBracketCommand(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand;
