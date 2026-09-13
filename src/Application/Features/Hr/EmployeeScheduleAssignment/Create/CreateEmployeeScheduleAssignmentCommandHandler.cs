namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateEmployeeScheduleAssignmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateEmployeeScheduleAssignmentCommand, Result<EmployeeScheduleAssignmentResponse>>
{
    public async Task<Result<EmployeeScheduleAssignmentResponse>> HandleAsync(CreateEmployeeScheduleAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeScheduleAssignmentResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_SCHEDULES", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<EmployeeScheduleAssignmentResponse>(Error.Forbidden("EmployeeScheduleAssignment.Forbidden", "You do not have permission to assign schedules for this branch."));

        var scheduleExists = await dbContext.WorkSchedules.AnyAsync(w => w.Id == command.WorkScheduleId, cancellationToken);
        if (!scheduleExists)
            return Result.Failure<EmployeeScheduleAssignmentResponse>(Error.NotFound("WorkSchedule.NotFound", $"Work schedule with ID '{command.WorkScheduleId}' was not found."));

        // Automatically closes any still-open prior assignment for this employee the day before
        // the new one takes effect, so DTR's "effective assignment for this date" lookup never
        // finds two open-ended rows for the same employee.
        var openAssignment = await dbContext.EmployeeScheduleAssignments
            .Where(a => a.EmployeeId == command.EmployeeId && a.EndDate == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (openAssignment is not null)
            openAssignment.EndDate = command.EffectiveDate.AddDays(-1);

        var assignment = new EmployeeScheduleAssignment
        {
            EmployeeId = command.EmployeeId,
            WorkScheduleId = command.WorkScheduleId,
            EffectiveDate = command.EffectiveDate,
            EndDate = command.EndDate
        };

        dbContext.EmployeeScheduleAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeScheduleAssignmentResponse(assignment.Id, assignment.EmployeeId, assignment.WorkScheduleId, assignment.EffectiveDate, assignment.EndDate, assignment.CreatedAt);
        return Result.Success(response);
    }
}
