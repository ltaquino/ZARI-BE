namespace ZARI.Application.Features.Hr.EmployeeDeductions.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateEmployeeDeductionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateEmployeeDeductionCommand>
{
    public async Task<Result> HandleAsync(UpdateEmployeeDeductionCommand command, CancellationToken cancellationToken = default)
    {
        var deduction = await dbContext.EmployeeDeductions.FindAsync([command.Id], cancellationToken);
        if (deduction is null)
            return Result.Failure(Error.NotFound("EmployeeDeduction.NotFound", $"Employee deduction with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([deduction.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_DEDUCTIONS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeDeduction.Forbidden", "You do not have permission to update employee deductions for this branch."));

        deduction.Code = command.Code;
        deduction.Name = command.Name;
        deduction.Amount = command.Amount;
        deduction.EffectiveDate = command.EffectiveDate;
        deduction.EndDate = command.EndDate;
        deduction.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
