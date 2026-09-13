namespace ZARI.Application.Features.Hr.WorkSchedules.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteWorkScheduleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteWorkScheduleCommand>
{
    public async Task<Result> HandleAsync(DeleteWorkScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await dbContext.WorkSchedules.FindAsync([command.Id], cancellationToken);
        if (schedule is null)
            return Result.Failure(Error.NotFound("WorkSchedule.NotFound", $"Work schedule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("WORK_SCHEDULES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("WorkSchedule.Forbidden", "You do not have permission to delete work schedules."));

        var assignmentCount = await dbContext.EmployeeScheduleAssignments.CountAsync(a => a.WorkScheduleId == command.Id, cancellationToken);
        if (assignmentCount > 0)
            return Result.Failure(Error.Conflict("WorkSchedule.HasAssignments", $"Cannot delete this work schedule — it has {assignmentCount} employee assignment{(assignmentCount == 1 ? "" : "s")}."));

        dbContext.WorkSchedules.Remove(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
