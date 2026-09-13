namespace ZARI.Application.Features.Hr.LeaveRequests.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetLeaveRequestQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLeaveRequestQuery, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> HandleAsync(GetLeaveRequestQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.LeaveRequests
            .Where(r => r.Id == query.Id)
            .Select(r => new { r.Id, r.RequestNo, r.EmployeeId, r.LeaveTypeId, r.DateFrom, r.DateTo, r.Days, r.Reason, r.AttachmentUrl, r.Status, r.CreatedAt, r.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<LeaveRequestResponse>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to view leave requests for this branch."));

        var response = new LeaveRequestResponse(row.Id, row.RequestNo, row.EmployeeId, row.LeaveTypeId, row.DateFrom, row.DateTo, row.Days, row.Reason, row.AttachmentUrl, row.Status, row.CreatedAt);
        return Result.Success(response);
    }
}
