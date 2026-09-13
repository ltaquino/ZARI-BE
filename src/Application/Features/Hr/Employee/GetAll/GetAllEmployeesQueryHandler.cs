namespace ZARI.Application.Features.Hr.Employees.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Domain.Common;

public sealed class GetAllEmployeesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllEmployeesQuery, Result<List<EmployeeResponse>>>
{
    public async Task<Result<List<EmployeeResponse>>> HandleAsync(GetAllEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EMPLOYEES", FormAction.View, cancellationToken))
            return Result.Failure<List<EmployeeResponse>>(Error.Forbidden("Employee.Forbidden", "You do not have permission to view employees."));

        var items = await dbContext.Employees.AsNoTracking()
            .OrderBy(e => e.EmployeeNo)
            .Select(e => new EmployeeResponse(e.Id, e.EmployeeNo, e.FirstName, e.MiddleName, e.LastName, e.Suffix,
                e.DateOfBirth, e.Sex, e.CivilStatus, e.Address, e.BranchId, e.PositionId, e.DepartmentId, e.DateHired,
                e.Status, e.EmploymentStatus, e.RegularizationDate, e.SeparationDate, e.SeparationReason,
                e.SssNo, e.PhilHealthNo, e.PagIbigNo, e.Tin, e.BasicSalary, e.BankAccountNo, e.BankName,
                e.BarcodeNo, e.RfidTagNo, e.ApplicationUserId, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
