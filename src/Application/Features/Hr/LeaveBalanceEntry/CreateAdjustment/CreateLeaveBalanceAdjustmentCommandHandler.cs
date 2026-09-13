namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.CreateAdjustment;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared;
using ZARI.Domain.Common;

public sealed class CreateLeaveBalanceAdjustmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateLeaveBalanceAdjustmentCommand, Result<LeaveBalanceEntryResponse>>
{
    public async Task<Result<LeaveBalanceEntryResponse>> HandleAsync(CreateLeaveBalanceAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<LeaveBalanceEntryResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_BALANCE_ENTRIES", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<LeaveBalanceEntryResponse>(Error.Forbidden("LeaveBalanceEntry.Forbidden", "You do not have permission to post leave balance entries for this branch."));

        var leaveTypeExists = await dbContext.LeaveTypes.AnyAsync(l => l.Id == command.LeaveTypeId, cancellationToken);
        if (!leaveTypeExists)
            return Result.Failure<LeaveBalanceEntryResponse>(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{command.LeaveTypeId}' was not found."));

        var entry = await LeaveBalanceLedgerService.PostEntryAsync(
            dbContext, command.EmployeeId, command.LeaveTypeId, command.Date, command.Type, command.Days,
            command.ReferenceTable, command.ReferenceId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new LeaveBalanceEntryResponse(entry.Id, entry.EmployeeId, entry.LeaveTypeId, entry.Date, entry.Type, entry.Days, entry.RunningBalance, entry.ReferenceTable, entry.ReferenceId, entry.CreatedAt);
        return Result.Success(response);
    }
}
