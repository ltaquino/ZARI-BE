namespace ZARI.Application.Features.Hr.EmployeeAllowances.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteEmployeeAllowanceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteEmployeeAllowanceCommand>
{
    public async Task<Result> HandleAsync(DeleteEmployeeAllowanceCommand command, CancellationToken cancellationToken = default)
    {
        var allowance = await dbContext.EmployeeAllowances.FindAsync([command.Id], cancellationToken);
        if (allowance is null)
            return Result.Failure(Error.NotFound("EmployeeAllowance.NotFound", $"Employee allowance with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([allowance.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_ALLOWANCES", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeAllowance.Forbidden", "You do not have permission to delete employee allowances for this branch."));

        dbContext.EmployeeAllowances.Remove(allowance);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
