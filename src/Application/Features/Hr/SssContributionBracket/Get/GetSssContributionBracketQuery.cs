namespace ZARI.Application.Features.Hr.SssContributionBrackets.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetSssContributionBracketQuery(Guid Id) : IQuery<Result<SssContributionBracketResponse>>;

public sealed record SssContributionBracketResponse(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal EmployeeShare,
    decimal EmployerShare,
    DateTimeOffset EffectiveDate,
    string Status,
    DateTimeOffset CreatedAt);
