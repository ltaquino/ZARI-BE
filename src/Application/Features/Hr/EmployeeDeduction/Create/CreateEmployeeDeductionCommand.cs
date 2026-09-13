namespace ZARI.Application.Features.Hr.EmployeeDeductions.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Domain.Common;

public sealed record CreateEmployeeDeductionCommand(
    Guid EmployeeId,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status) : ICommand<Result<EmployeeDeductionResponse>>;
