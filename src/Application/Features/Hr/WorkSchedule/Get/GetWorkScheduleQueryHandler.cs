namespace ZARI.Application.Features.Hr.WorkSchedules.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetWorkScheduleQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetWorkScheduleQuery, Result<WorkScheduleResponse>>
{
    public async Task<Result<WorkScheduleResponse>> HandleAsync(GetWorkScheduleQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WORK_SCHEDULES", FormAction.View, cancellationToken))
            return Result.Failure<WorkScheduleResponse>(Error.Forbidden("WorkSchedule.Forbidden", "You do not have permission to view work schedules."));

        var schedule = await dbContext.WorkSchedules
            .Where(w => w.Id == query.Id)
            .Select(w => new WorkScheduleResponse(w.Id, w.Code, w.Name, w.TimeIn, w.TimeOut, w.WorkDays, w.GracePeriodMinutes, w.BreakMinutes, w.Status, w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (schedule is null)
            return Result.Failure<WorkScheduleResponse>(Error.NotFound("WorkSchedule.NotFound", $"Work schedule with ID '{query.Id}' was not found."));

        return Result.Success(schedule);
    }
}
