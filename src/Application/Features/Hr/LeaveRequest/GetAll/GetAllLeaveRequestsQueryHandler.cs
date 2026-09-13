namespace ZARI.Application.Features.Hr.LeaveRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Domain.Common;

public sealed class GetAllLeaveRequestsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllLeaveRequestsQuery, Result<List<LeaveRequestResponse>>>
{
    public async Task<Result<List<LeaveRequestResponse>>> HandleAsync(GetAllLeaveRequestsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LEAVE_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<List<LeaveRequestResponse>>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to view leave requests."));

        var items = await dbContext.LeaveRequests.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LeaveRequestResponse(r.Id, r.RequestNo, r.EmployeeId, r.LeaveTypeId, r.DateFrom, r.DateTo, r.Days, r.Reason, r.AttachmentUrl, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
