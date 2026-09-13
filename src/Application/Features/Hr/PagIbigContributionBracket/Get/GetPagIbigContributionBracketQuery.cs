namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPagIbigContributionBracketQuery(Guid Id) : IQuery<Result<PagIbigContributionBracketResponse>>;

public sealed record PagIbigContributionBracketResponse(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status,
    DateTimeOffset CreatedAt);
