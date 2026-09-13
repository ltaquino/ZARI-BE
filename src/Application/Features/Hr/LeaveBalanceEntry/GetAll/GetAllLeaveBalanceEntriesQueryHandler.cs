namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Domain.Common;

public sealed class GetAllLeaveBalanceEntriesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllLeaveBalanceEntriesQuery, Result<List<LeaveBalanceEntryResponse>>>
{
    public async Task<Result<List<LeaveBalanceEntryResponse>>> HandleAsync(GetAllLeaveBalanceEntriesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LEAVE_BALANCE_ENTRIES", FormAction.View, cancellationToken))
            return Result.Failure<List<LeaveBalanceEntryResponse>>(Error.Forbidden("LeaveBalanceEntry.Forbidden", "You do not have permission to view leave balance entries."));

        var items = await dbContext.LeaveBalanceEntries.AsNoTracking()
            .OrderByDescending(e => e.Date)
            .Select(e => new LeaveBalanceEntryResponse(e.Id, e.EmployeeId, e.LeaveTypeId, e.Date, e.Type, e.Days, e.RunningBalance, e.ReferenceTable, e.ReferenceId, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
