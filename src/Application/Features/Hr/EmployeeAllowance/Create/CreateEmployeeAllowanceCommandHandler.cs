namespace ZARI.Application.Features.Hr.EmployeeAllowances.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateEmployeeAllowanceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateEmployeeAllowanceCommand, Result<EmployeeAllowanceResponse>>
{
    public async Task<Result<EmployeeAllowanceResponse>> HandleAsync(CreateEmployeeAllowanceCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeAllowanceResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_ALLOWANCES", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<EmployeeAllowanceResponse>(Error.Forbidden("EmployeeAllowance.Forbidden", "You do not have permission to create employee allowances for this branch."));

        var allowance = new EmployeeAllowance
        {
            EmployeeId = command.EmployeeId,
            Code = command.Code,
            Name = command.Name,
            Amount = command.Amount,
            EffectiveDate = command.EffectiveDate,
            EndDate = command.EndDate,
            Status = command.Status
        };

        dbContext.EmployeeAllowances.Add(allowance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeAllowanceResponse(allowance.Id, allowance.EmployeeId, allowance.Code, allowance.Name, allowance.Amount, allowance.EffectiveDate, allowance.EndDate, allowance.Status, allowance.CreatedAt);
        return Result.Success(response);
    }
}
