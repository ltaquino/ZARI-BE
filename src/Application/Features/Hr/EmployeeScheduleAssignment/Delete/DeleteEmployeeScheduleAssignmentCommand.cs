namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteEmployeeScheduleAssignmentCommand(Guid Id) : ICommand;
