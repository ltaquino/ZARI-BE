namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Domain.Common;

public sealed record GetAllEmployeeScheduleAssignmentsQuery : IQuery<Result<List<EmployeeScheduleAssignmentResponse>>>;
