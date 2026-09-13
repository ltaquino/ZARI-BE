namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteEmployeeScheduleAssignmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteEmployeeScheduleAssignmentCommand>
{
    public async Task<Result> HandleAsync(DeleteEmployeeScheduleAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.EmployeeScheduleAssignments.FindAsync([command.Id], cancellationToken);
        if (assignment is null)
            return Result.Failure(Error.NotFound("EmployeeScheduleAssignment.NotFound", $"Schedule assignment with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([assignment.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_SCHEDULES", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeScheduleAssignment.Forbidden", "You do not have permission to delete schedule assignments for this branch."));

        dbContext.EmployeeScheduleAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
