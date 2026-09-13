namespace ZARI.Application.Features.Hr.EmployeeDeductions.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetEmployeeDeductionQuery(Guid Id) : IQuery<Result<EmployeeDeductionResponse>>;

public sealed record EmployeeDeductionResponse(
    Guid Id,
    Guid EmployeeId,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status,
    DateTimeOffset CreatedAt);
