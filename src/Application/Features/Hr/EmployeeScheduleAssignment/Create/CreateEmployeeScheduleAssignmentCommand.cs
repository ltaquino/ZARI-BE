namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Domain.Common;

public sealed record CreateEmployeeScheduleAssignmentCommand(
    Guid EmployeeId,
    Guid WorkScheduleId,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate) : ICommand<Result<EmployeeScheduleAssignmentResponse>>;
