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

        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
