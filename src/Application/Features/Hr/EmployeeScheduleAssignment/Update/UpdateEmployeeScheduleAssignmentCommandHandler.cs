namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateEmployeeScheduleAssignmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateEmployeeScheduleAssignmentCommand>
{
    public async Task<Result> HandleAsync(UpdateEmployeeScheduleAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.EmployeeScheduleAssignments.FindAsync([command.Id], cancellationToken);
        if (assignment is null)
            return Result.Failure(Error.NotFound("EmployeeScheduleAssignment.NotFound", $"Schedule assignment with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([assignment.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_SCHEDULES", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeScheduleAssignment.Forbidden", "You do not have permission to update schedule assignments for this branch."));

        var scheduleExists = await dbContext.WorkSchedules.AnyAsync(w => w.Id == command.WorkScheduleId, cancellationToken);
        if (!scheduleExists)
            return Result.Failure(Error.NotFound("WorkSchedule.NotFound", $"Work schedule with ID '{command.WorkScheduleId}' was not found."));

        assignment.WorkScheduleId = command.WorkScheduleId;
        assignment.EffectiveDate = command.EffectiveDate;
        assignment.EndDate = command.EndDate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
