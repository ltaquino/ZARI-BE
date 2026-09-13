namespace ZARI.Application.Features.Hr.WorkSchedules.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateWorkScheduleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateWorkScheduleCommand>
{
    public async Task<Result> HandleAsync(UpdateWorkScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var schedule = await dbContext.WorkSchedules.FindAsync([command.Id], cancellationToken);
        if (schedule is null)
            return Result.Failure(Error.NotFound("WorkSchedule.NotFound", $"Work schedule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("WORK_SCHEDULES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("WorkSchedule.Forbidden", "You do not have permission to update work schedules."));

        var duplicateCode = await dbContext.WorkSchedules
            .AnyAsync(w => w.Id != command.Id && w.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("WorkSchedule.DuplicateCode", $"A work schedule with code '{command.Code}' already exists."));

        schedule.Code = command.Code;
        schedule.Name = command.Name;
        schedule.TimeIn = command.TimeIn;
        schedule.TimeOut = command.TimeOut;
        schedule.WorkDays = command.WorkDays;
        schedule.GracePeriodMinutes = command.GracePeriodMinutes;
        schedule.BreakMinutes = command.BreakMinutes;
        schedule.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
