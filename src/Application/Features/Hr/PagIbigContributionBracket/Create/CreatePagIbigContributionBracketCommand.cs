namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed record CreatePagIbigContributionBracketCommand(
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand<Result<PagIbigContributionBracketResponse>>;
