namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetLeaveBalanceEntryQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLeaveBalanceEntryQuery, Result<LeaveBalanceEntryResponse>>
{
    public async Task<Result<LeaveBalanceEntryResponse>> HandleAsync(GetLeaveBalanceEntryQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.LeaveBalanceEntries
            .Where(e => e.Id == query.Id)
            .Select(e => new { e.Id, e.EmployeeId, e.LeaveTypeId, e.Date, e.Type, e.Days, e.RunningBalance, e.ReferenceTable, e.ReferenceId, e.CreatedAt, e.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<LeaveBalanceEntryResponse>(Error.NotFound("LeaveBalanceEntry.NotFound", $"Leave balance entry with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_BALANCE_ENTRIES", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<LeaveBalanceEntryResponse>(Error.Forbidden("LeaveBalanceEntry.Forbidden", "You do not have permission to view leave balance entries for this branch."));

        var response = new LeaveBalanceEntryResponse(row.Id, row.EmployeeId, row.LeaveTypeId, row.Date, row.Type, row.Days, row.RunningBalance, row.ReferenceTable, row.ReferenceId, row.CreatedAt);
        return Result.Success(response);
    }
}
