namespace ZARI.Application.Features.Hr.Employees.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetEmployeeQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetEmployeeQuery, Result<EmployeeResponse>>
{
    public async Task<Result<EmployeeResponse>> HandleAsync(GetEmployeeQuery query, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Where(e => e.Id == query.Id)
            .Select(e => new EmployeeResponse(e.Id, e.EmployeeNo, e.FirstName, e.MiddleName, e.LastName, e.Suffix,
                e.DateOfBirth, e.Sex, e.CivilStatus, e.Address, e.BranchId, e.PositionId, e.DepartmentId, e.DateHired,
                e.Status, e.EmploymentStatus, e.RegularizationDate, e.SeparationDate, e.SeparationReason,
                e.SssNo, e.PhilHealthNo, e.PagIbigNo, e.Tin, e.BasicSalary, e.BankAccountNo, e.BankName,
                e.BarcodeNo, e.RfidTagNo, e.ApplicationUserId, e.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
            return Result.Failure<EmployeeResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.View, employee.BranchId, cancellationToken))
            return Result.Failure<EmployeeResponse>(Error.Forbidden("Employee.Forbidden", "You do not have permission to view employees for this branch."));

        return Result.Success(employee);
    }
}
