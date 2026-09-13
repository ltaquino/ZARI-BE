namespace ZARI.Application.Features.Hr.EmployeeAllowances.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateEmployeeAllowanceCommand(
    Guid Id,
    string Code,
    string Name,
    decimal Amount,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    string Status) : ICommand;
