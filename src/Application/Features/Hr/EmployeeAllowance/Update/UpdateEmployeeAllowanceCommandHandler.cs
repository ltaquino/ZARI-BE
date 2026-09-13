namespace ZARI.Application.Features.Hr.EmployeeAllowances.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateEmployeeAllowanceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateEmployeeAllowanceCommand>
{
    public async Task<Result> HandleAsync(UpdateEmployeeAllowanceCommand command, CancellationToken cancellationToken = default)
    {
        var allowance = await dbContext.EmployeeAllowances.FindAsync([command.Id], cancellationToken);
        if (allowance is null)
            return Result.Failure(Error.NotFound("EmployeeAllowance.NotFound", $"Employee allowance with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([allowance.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_ALLOWANCES", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeAllowance.Forbidden", "You do not have permission to update employee allowances for this branch."));

        allowance.Code = command.Code;
        allowance.Name = command.Name;
        allowance.Amount = command.Amount;
        allowance.EffectiveDate = command.EffectiveDate;
        allowance.EndDate = command.EndDate;
        allowance.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
