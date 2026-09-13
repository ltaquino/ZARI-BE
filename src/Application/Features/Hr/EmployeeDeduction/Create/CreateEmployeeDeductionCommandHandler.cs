namespace ZARI.Application.Features.Hr.EmployeeDeductions.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateEmployeeDeductionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateEmployeeDeductionCommand, Result<EmployeeDeductionResponse>>
{
    public async Task<Result<EmployeeDeductionResponse>> HandleAsync(CreateEmployeeDeductionCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeDeductionResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_DEDUCTIONS", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<EmployeeDeductionResponse>(Error.Forbidden("EmployeeDeduction.Forbidden", "You do not have permission to create employee deductions for this branch."));

        var deduction = new EmployeeDeduction
        {
            EmployeeId = command.EmployeeId,
            Code = command.Code,
            Name = command.Name,
            Amount = command.Amount,
            EffectiveDate = command.EffectiveDate,
            EndDate = command.EndDate,
            Status = command.Status
        };

        dbContext.EmployeeDeductions.Add(deduction);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeDeductionResponse(deduction.Id, deduction.EmployeeId, deduction.Code, deduction.Name, deduction.Amount, deduction.EffectiveDate, deduction.EndDate, deduction.Status, deduction.CreatedAt);
        return Result.Success(response);
    }
}
