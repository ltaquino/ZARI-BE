namespace ZARI.Application.Features.Hr.WorkSchedules.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateWorkScheduleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateWorkScheduleCommand, Result<WorkScheduleResponse>>
{
    public async Task<Result<WorkScheduleResponse>> HandleAsync(CreateWorkScheduleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WORK_SCHEDULES", FormAction.Create, cancellationToken))
            return Result.Failure<WorkScheduleResponse>(Error.Forbidden("WorkSchedule.Forbidden", "You do not have permission to create work schedules."));

        var codeExists = await dbContext.WorkSchedules.AnyAsync(w => w.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<WorkScheduleResponse>(Error.Conflict("WorkSchedule.DuplicateCode", $"A work schedule with code '{command.Code}' already exists."));

        var schedule = new WorkSchedule
        {
            Code = command.Code,
            Name = command.Name,
            TimeIn = command.TimeIn,
            TimeOut = command.TimeOut,
            WorkDays = command.WorkDays,
            GracePeriodMinutes = command.GracePeriodMinutes,
            BreakMinutes = command.BreakMinutes,
            Status = command.Status
        };

        dbContext.WorkSchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorkScheduleResponse(schedule.Id, schedule.Code, schedule.Name, schedule.TimeIn, schedule.TimeOut, schedule.WorkDays, schedule.GracePeriodMinutes, schedule.BreakMinutes, schedule.Status, schedule.CreatedAt);
        return Result.Success(response);
    }
}
