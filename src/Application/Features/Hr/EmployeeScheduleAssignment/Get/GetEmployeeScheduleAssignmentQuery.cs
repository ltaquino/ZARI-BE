namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetEmployeeScheduleAssignmentQuery(Guid Id) : IQuery<Result<EmployeeScheduleAssignmentResponse>>;

public sealed record EmployeeScheduleAssignmentResponse(
    Guid Id,
    Guid EmployeeId,
    Guid WorkScheduleId,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    DateTimeOffset CreatedAt);
