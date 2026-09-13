namespace ZARI.Application.Features.Hr.LeaveTypes.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Domain.Common;

public sealed class GetAllLeaveTypesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllLeaveTypesQuery, Result<List<LeaveTypeResponse>>>
{
    public async Task<Result<List<LeaveTypeResponse>>> HandleAsync(GetAllLeaveTypesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LEAVE_TYPES", FormAction.View, cancellationToken))
            return Result.Failure<List<LeaveTypeResponse>>(Error.Forbidden("LeaveType.Forbidden", "You do not have permission to view leave types."));

        var items = await dbContext.LeaveTypes.AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new LeaveTypeResponse(l.Id, l.Code, l.Name, l.AccrualFrequency, l.AccrualAmount, l.MaxCarryOverDays, l.IsPaid, l.RequiresDocumentation, l.Status, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
