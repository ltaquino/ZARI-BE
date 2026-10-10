namespace ZARI.Application.Features.Hr.EmployeeDeductions.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteEmployeeDeductionCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteEmployeeDeductionCommand>
{
    public async Task<Result> HandleAsync(DeleteEmployeeDeductionCommand command, CancellationToken cancellationToken = default)
    {
        var deduction = await dbContext.EmployeeDeductions.FindAsync([command.Id], cancellationToken);
        if (deduction is null)
            return Result.Failure(Error.NotFound("EmployeeDeduction.NotFound", $"Employee deduction with ID '{command.Id}' was not found."));
        // Its amount is already booked to 1220 Due from Employees by the trip close; payroll clears it from there.
        if (ZARI.Domain.Entities.EmployeeDeduction.IsSystemCode(deduction.Code))
            return Result.Failure(Error.Validation("EmployeeDeduction.TripShortage",
                "This deduction is a trip shortage charged at a route-sales trip close — it can't be deleted here." ));

        var employee = await dbContext.Employees.FindAsync([deduction.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("EMPLOYEE_DEDUCTIONS", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("EmployeeDeduction.Forbidden", "You do not have permission to delete employee deductions for this branch."));

        dbContext.EmployeeDeductions.Remove(deduction);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
