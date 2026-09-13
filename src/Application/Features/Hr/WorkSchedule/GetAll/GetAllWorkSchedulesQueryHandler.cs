namespace ZARI.Application.Features.Hr.WorkSchedules.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Domain.Common;

public sealed class GetAllWorkSchedulesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllWorkSchedulesQuery, Result<List<WorkScheduleResponse>>>
{
    public async Task<Result<List<WorkScheduleResponse>>> HandleAsync(GetAllWorkSchedulesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WORK_SCHEDULES", FormAction.View, cancellationToken))
            return Result.Failure<List<WorkScheduleResponse>>(Error.Forbidden("WorkSchedule.Forbidden", "You do not have permission to view work schedules."));

        var items = await dbContext.WorkSchedules.AsNoTracking()
            .OrderBy(w => w.Code)
            .Select(w => new WorkScheduleResponse(w.Id, w.Code, w.Name, w.TimeIn, w.TimeOut, w.WorkDays, w.GracePeriodMinutes, w.BreakMinutes, w.Status, w.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
