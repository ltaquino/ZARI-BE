namespace ZARI.Application.Features.Hr.Departments.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDepartmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteDepartmentCommand>
{
    public async Task<Result> HandleAsync(DeleteDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var department = await dbContext.Departments.FindAsync([command.Id], cancellationToken);
        if (department is null)
            return Result.Failure(Error.NotFound("Department.NotFound", $"Department with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("DEPARTMENTS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Department.Forbidden", "You do not have permission to delete departments."));

        var positionCount = await dbContext.Positions.CountAsync(p => p.DepartmentId == command.Id, cancellationToken);
        if (positionCount > 0)
            return Result.Failure(Error.Conflict("Department.HasPositions", $"Cannot delete this department — it has {positionCount} position{(positionCount == 1 ? "" : "s")}."));

        var employeeCount = await dbContext.Employees.CountAsync(e => e.DepartmentId == command.Id, cancellationToken);
        if (employeeCount > 0)
            return Result.Failure(Error.Conflict("Department.HasEmployees", $"Cannot delete this department — it has {employeeCount} employee{(employeeCount == 1 ? "" : "s")}."));

        dbContext.Departments.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
