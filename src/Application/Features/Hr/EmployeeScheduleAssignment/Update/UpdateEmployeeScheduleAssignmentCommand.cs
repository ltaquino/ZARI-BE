namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateEmployeeScheduleAssignmentCommand(
    Guid Id,
    Guid WorkScheduleId,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate) : ICommand;
