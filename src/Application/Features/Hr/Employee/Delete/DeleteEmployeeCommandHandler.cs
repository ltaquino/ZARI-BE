namespace ZARI.Application.Features.Hr.Employees.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteEmployeeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteEmployeeCommand>
{
    public async Task<Result> HandleAsync(DeleteEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.Id], cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.Delete, employee.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Employee.Forbidden", "You do not have permission to delete employees for this branch."));

        var headOfDepartmentCount = await dbContext.Departments.CountAsync(d => d.HeadEmployeeId == command.Id, cancellationToken);
        if (headOfDepartmentCount > 0)
            return Result.Failure(Error.Conflict("Employee.IsDepartmentHead", "Cannot delete this employee — they are set as the head of a department. Reassign the department head first."));

        var scheduleAssignmentCount = await dbContext.EmployeeScheduleAssignments.CountAsync(a => a.EmployeeId == command.Id, cancellationToken);
        if (scheduleAssignmentCount > 0)
            return Result.Failure(Error.Conflict("Employee.HasScheduleAssignments", $"Cannot delete this employee — they have {scheduleAssignmentCount} schedule assignment{(scheduleAssignmentCount == 1 ? "" : "s")}."));

        var dtrCount = await dbContext.DailyTimeRecords.CountAsync(d => d.EmployeeId == command.Id, cancellationToken);
        if (dtrCount > 0)
            return Result.Failure(Error.Conflict("Employee.HasDailyTimeRecords", $"Cannot delete this employee — they have {dtrCount} daily time record{(dtrCount == 1 ? "" : "s")}."));

        var leaveRequestCount = await dbContext.LeaveRequests.CountAsync(r => r.EmployeeId == command.Id, cancellationToken);
        if (leaveRequestCount > 0)
            return Result.Failure(Error.Conflict("Employee.HasLeaveRequests", $"Cannot delete this employee — they have {leaveRequestCount} leave request{(leaveRequestCount == 1 ? "" : "s")}."));

        var leaveBalanceEntryCount = await dbContext.LeaveBalanceEntries.CountAsync(e => e.EmployeeId == command.Id, cancellationToken);
        if (leaveBalanceEntryCount > 0)
            return Result.Failure(Error.Conflict("Employee.HasLeaveBalanceEntries", $"Cannot delete this employee — they have {leaveBalanceEntryCount} leave balance ledger entr{(leaveBalanceEntryCount == 1 ? "y" : "ies")}."));

        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
