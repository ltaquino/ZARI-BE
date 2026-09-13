namespace ZARI.Application.Features.Hr.EmployeeDeductions.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateEmployeeDeductionCommand(
    Guid Id,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status) : ICommand;
