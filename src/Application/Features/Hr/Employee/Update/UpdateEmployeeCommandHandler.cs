namespace ZARI.Application.Features.Hr.Employees.Update;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateEmployeeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
    : ICommandHandler<UpdateEmployeeCommand>
{
    public async Task<Result> HandleAsync(UpdateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.Id], cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.Edit, employee.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Employee.Forbidden", "You do not have permission to update employees for this branch."));

        var duplicateEmployeeNo = await dbContext.Employees.AnyAsync(e => e.Id != command.Id && e.EmployeeNo == command.EmployeeNo, cancellationToken);
        if (duplicateEmployeeNo)
            return Result.Failure(Error.Conflict("Employee.DuplicateEmployeeNo", $"An employee with number '{command.EmployeeNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.BarcodeNo) &&
            await dbContext.Employees.AnyAsync(e => e.Id != command.Id && e.BarcodeNo == command.BarcodeNo, cancellationToken))
            return Result.Failure(Error.Conflict("Employee.DuplicateBarcodeNo", $"An employee with barcode '{command.BarcodeNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.RfidTagNo) &&
            await dbContext.Employees.AnyAsync(e => e.Id != command.Id && e.RfidTagNo == command.RfidTagNo, cancellationToken))
            return Result.Failure(Error.Conflict("Employee.DuplicateRfidTagNo", $"An employee with RFID tag '{command.RfidTagNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.ApplicationUserId) &&
            await dbContext.Employees.AnyAsync(e => e.Id != command.Id && e.ApplicationUserId == command.ApplicationUserId, cancellationToken))
            return Result.Failure(Error.Conflict("Employee.DuplicateApplicationUser", "This system login is already linked to another employee."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var positionExists = await dbContext.Positions.AnyAsync(p => p.Id == command.PositionId, cancellationToken);
        if (!positionExists)
            return Result.Failure(Error.NotFound("Position.NotFound", $"Position with ID '{command.PositionId}' was not found."));

        var departmentExists = await dbContext.Departments.AnyAsync(d => d.Id == command.DepartmentId, cancellationToken);
        if (!departmentExists)
            return Result.Failure(Error.NotFound("Department.NotFound", $"Department with ID '{command.DepartmentId}' was not found."));

        if (!string.IsNullOrWhiteSpace(command.ApplicationUserId))
        {
            var user = await userManager.FindByIdAsync(command.ApplicationUserId);
            if (user is null)
                return Result.Failure(Error.NotFound("ApplicationUser.NotFound", $"System user with ID '{command.ApplicationUserId}' was not found."));
        }

        employee.EmployeeNo = command.EmployeeNo;
        employee.FirstName = command.FirstName;
        employee.MiddleName = command.MiddleName;
        employee.LastName = command.LastName;
        employee.Suffix = command.Suffix;
        employee.DateOfBirth = command.DateOfBirth;
        employee.Sex = command.Sex;
        employee.CivilStatus = command.CivilStatus;
        employee.Address = command.Address;
        employee.BranchId = command.BranchId;
        employee.PositionId = command.PositionId;
        employee.DepartmentId = command.DepartmentId;
        employee.DateHired = command.DateHired;
        employee.Status = command.Status;
        employee.EmploymentStatus = command.EmploymentStatus;
        employee.RegularizationDate = command.RegularizationDate;
        employee.SeparationDate = command.SeparationDate;
        employee.SeparationReason = command.SeparationReason;
        employee.SssNo = command.SssNo;
        employee.PhilHealthNo = command.PhilHealthNo;
        employee.PagIbigNo = command.PagIbigNo;
        employee.Tin = command.Tin;
        employee.BasicSalary = command.BasicSalary;
        employee.BankAccountNo = command.BankAccountNo;
        employee.BankName = command.BankName;
        employee.BarcodeNo = command.BarcodeNo;
        employee.RfidTagNo = command.RfidTagNo;
        employee.ApplicationUserId = command.ApplicationUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
