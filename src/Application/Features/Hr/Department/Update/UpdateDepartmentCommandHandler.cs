namespace ZARI.Application.Features.Hr.Departments.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateDepartmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateDepartmentCommand>
{
    public async Task<Result> HandleAsync(UpdateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var department = await dbContext.Departments.FindAsync([command.Id], cancellationToken);
        if (department is null)
            return Result.Failure(Error.NotFound("Department.NotFound", $"Department with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("DEPARTMENTS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Department.Forbidden", "You do not have permission to update departments."));

        var duplicateCode = await dbContext.Departments
            .AnyAsync(d => d.Id != command.Id && d.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("Department.DuplicateCode", $"A department with code '{command.Code}' already exists."));

        if (command.HeadEmployeeId is not null)
        {
            var headExists = await dbContext.Employees.AnyAsync(e => e.Id == command.HeadEmployeeId, cancellationToken);
            if (!headExists)
                return Result.Failure(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.HeadEmployeeId}' was not found."));
        }

        department.Code = command.Code;
        department.Name = command.Name;
        department.Status = command.Status;
        department.HeadEmployeeId = command.HeadEmployeeId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
