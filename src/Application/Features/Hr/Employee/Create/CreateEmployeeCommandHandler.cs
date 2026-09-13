namespace ZARI.Application.Features.Hr.Employees.Create;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateEmployeeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
    : ICommandHandler<CreateEmployeeCommand, Result<EmployeeResponse>>
{
    public async Task<Result<EmployeeResponse>> HandleAsync(CreateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<EmployeeResponse>(Error.Forbidden("Employee.Forbidden", "You do not have permission to create employees for this branch."));

        var employeeNoExists = await dbContext.Employees.AnyAsync(e => e.EmployeeNo == command.EmployeeNo, cancellationToken);
        if (employeeNoExists)
            return Result.Failure<EmployeeResponse>(Error.Conflict("Employee.DuplicateEmployeeNo", $"An employee with number '{command.EmployeeNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.BarcodeNo) && await dbContext.Employees.AnyAsync(e => e.BarcodeNo == command.BarcodeNo, cancellationToken))
            return Result.Failure<EmployeeResponse>(Error.Conflict("Employee.DuplicateBarcodeNo", $"An employee with barcode '{command.BarcodeNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.RfidTagNo) && await dbContext.Employees.AnyAsync(e => e.RfidTagNo == command.RfidTagNo, cancellationToken))
            return Result.Failure<EmployeeResponse>(Error.Conflict("Employee.DuplicateRfidTagNo", $"An employee with RFID tag '{command.RfidTagNo}' already exists."));

        if (!string.IsNullOrWhiteSpace(command.ApplicationUserId) && await dbContext.Employees.AnyAsync(e => e.ApplicationUserId == command.ApplicationUserId, cancellationToken))
            return Result.Failure<EmployeeResponse>(Error.Conflict("Employee.DuplicateApplicationUser", "This system login is already linked to another employee."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<EmployeeResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var position = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == command.PositionId, cancellationToken);
        if (position is null)
            return Result.Failure<EmployeeResponse>(Error.NotFound("Position.NotFound", $"Position with ID '{command.PositionId}' was not found."));

        if (position.PayGradeMin is { } min && command.BasicSalary < min)
            return Result.Failure<EmployeeResponse>(Error.Validation("Employee.BelowPayGrade", $"Basic salary {command.BasicSalary:N2} is below this position's minimum pay grade of {min:N2}."));
        if (position.PayGradeMax is { } max && command.BasicSalary > max)
            return Result.Failure<EmployeeResponse>(Error.Validation("Employee.AbovePayGrade", $"Basic salary {command.BasicSalary:N2} is above this position's maximum pay grade of {max:N2}."));

        var departmentExists = await dbContext.Departments.AnyAsync(d => d.Id == command.DepartmentId, cancellationToken);
        if (!departmentExists)
            return Result.Failure<EmployeeResponse>(Error.NotFound("Department.NotFound", $"Department with ID '{command.DepartmentId}' was not found."));

        if (!string.IsNullOrWhiteSpace(command.ApplicationUserId))
        {
            var user = await userManager.FindByIdAsync(command.ApplicationUserId);
            if (user is null)
                return Result.Failure<EmployeeResponse>(Error.NotFound("ApplicationUser.NotFound", $"System user with ID '{command.ApplicationUserId}' was not found."));
        }

        var employee = new Employee
        {
            EmployeeNo = command.EmployeeNo,
            FirstName = command.FirstName,
            MiddleName = command.MiddleName,
            LastName = command.LastName,
            Suffix = command.Suffix,
            DateOfBirth = command.DateOfBirth,
            Sex = command.Sex,
            CivilStatus = command.CivilStatus,
            Address = command.Address,
            BranchId = command.BranchId,
            PositionId = command.PositionId,
            DepartmentId = command.DepartmentId,
            DateHired = command.DateHired,
            Status = command.Status,
            EmploymentStatus = command.EmploymentStatus,
            SssNo = command.SssNo,
            PhilHealthNo = command.PhilHealthNo,
            PagIbigNo = command.PagIbigNo,
            Tin = command.Tin,
            BasicSalary = command.BasicSalary,
            BankAccountNo = command.BankAccountNo,
            BankName = command.BankName,
            BarcodeNo = command.BarcodeNo,
            RfidTagNo = command.RfidTagNo,
            ApplicationUserId = command.ApplicationUserId
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeResponse(employee.Id, employee.EmployeeNo, employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix,
            employee.DateOfBirth, employee.Sex, employee.CivilStatus, employee.Address, employee.BranchId, employee.PositionId, employee.DepartmentId, employee.DateHired,
            employee.Status, employee.EmploymentStatus, employee.RegularizationDate, employee.SeparationDate, employee.SeparationReason,
            employee.SssNo, employee.PhilHealthNo, employee.PagIbigNo, employee.Tin, employee.BasicSalary, employee.BankAccountNo, employee.BankName,
            employee.BarcodeNo, employee.RfidTagNo, employee.ApplicationUserId, employee.CreatedAt);
        return Result.Success(response);
    }
}
