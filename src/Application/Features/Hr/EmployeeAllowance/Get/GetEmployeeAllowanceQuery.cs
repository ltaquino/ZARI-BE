namespace ZARI.Application.Features.Hr.EmployeeAllowances.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetEmployeeAllowanceQuery(Guid Id) : IQuery<Result<EmployeeAllowanceResponse>>;

public sealed record EmployeeAllowanceResponse(
    Guid Id,
    Guid EmployeeId,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status,
    DateTimeOffset CreatedAt);
