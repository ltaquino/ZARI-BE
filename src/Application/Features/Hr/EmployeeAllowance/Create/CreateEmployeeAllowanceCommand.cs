namespace ZARI.Application.Features.Hr.EmployeeAllowances.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Domain.Common;

public sealed record CreateEmployeeAllowanceCommand(
    Guid EmployeeId,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status) : ICommand<Result<EmployeeAllowanceResponse>>;
