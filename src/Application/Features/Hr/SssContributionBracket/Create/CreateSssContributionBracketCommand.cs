namespace ZARI.Application.Features.Hr.SssContributionBrackets.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed record CreateSssContributionBracketCommand(
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand<Result<SssContributionBracketResponse>>;
