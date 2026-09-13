namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetEmployeeScheduleAssignmentQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetEmployeeScheduleAssignmentQuery, Result<EmployeeScheduleAssignmentResponse>>
{
    public async Task<Result<EmployeeScheduleAssignmentResponse>> HandleAsync(GetEmployeeScheduleAssignmentQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.EmployeeScheduleAssignments
            .Where(a => a.Id == query.Id)
            .Select(a => new { a.Id, a.EmployeeId, a.WorkScheduleId, a.EffectiveDate, a.EndDate, a.CreatedAt, a.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<EmployeeScheduleAssignmentResponse>(Error.NotFound("EmployeeScheduleAssignment.NotFound", $"Schedule assignment with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_SCHEDULES", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<EmployeeScheduleAssignmentResponse>(Error.Forbidden("EmployeeScheduleAssignment.Forbidden", "You do not have permission to view schedule assignments for this branch."));

        var response = new EmployeeScheduleAssignmentResponse(row.Id, row.EmployeeId, row.WorkScheduleId, row.EffectiveDate, row.EndDate, row.CreatedAt);
        return Result.Success(response);
    }
}
