namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Domain.Common;

public sealed class GetAllEmployeeScheduleAssignmentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllEmployeeScheduleAssignmentsQuery, Result<List<EmployeeScheduleAssignmentResponse>>>
{
    public async Task<Result<List<EmployeeScheduleAssignmentResponse>>> HandleAsync(GetAllEmployeeScheduleAssignmentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EMPLOYEE_SCHEDULES", FormAction.View, cancellationToken))
            return Result.Failure<List<EmployeeScheduleAssignmentResponse>>(Error.Forbidden("EmployeeScheduleAssignment.Forbidden", "You do not have permission to view schedule assignments."));

        var items = await dbContext.EmployeeScheduleAssignments.AsNoTracking()
            .OrderByDescending(a => a.EffectiveDate)
            .Select(a => new EmployeeScheduleAssignmentResponse(a.Id, a.EmployeeId, a.WorkScheduleId, a.EffectiveDate, a.EndDate, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
