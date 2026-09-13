namespace ZARI.Application.Features.Hr.Departments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDepartmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> HandleAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DEPARTMENTS", FormAction.Create, cancellationToken))
            return Result.Failure<DepartmentResponse>(Error.Forbidden("Department.Forbidden", "You do not have permission to create departments."));

        var codeExists = await dbContext.Departments.AnyAsync(d => d.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<DepartmentResponse>(Error.Conflict("Department.DuplicateCode", $"A department with code '{command.Code}' already exists."));

        if (command.HeadEmployeeId is not null)
        {
            var headExists = await dbContext.Employees.AnyAsync(e => e.Id == command.HeadEmployeeId, cancellationToken);
            if (!headExists)
                return Result.Failure<DepartmentResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.HeadEmployeeId}' was not found."));
        }

        var department = new Department
        {
            Code = command.Code,
            Name = command.Name,
            Status = command.Status,
            HeadEmployeeId = command.HeadEmployeeId
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DepartmentResponse(department.Id, department.Code, department.Name, department.Status, department.HeadEmployeeId, department.CreatedAt);
        return Result.Success(response);
    }
}
