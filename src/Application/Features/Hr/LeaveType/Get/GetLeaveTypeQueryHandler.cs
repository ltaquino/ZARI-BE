namespace ZARI.Application.Features.Hr.LeaveTypes.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetLeaveTypeQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetLeaveTypeQuery, Result<LeaveTypeResponse>>
{
    public async Task<Result<LeaveTypeResponse>> HandleAsync(GetLeaveTypeQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LEAVE_TYPES", FormAction.View, cancellationToken))
            return Result.Failure<LeaveTypeResponse>(Error.Forbidden("LeaveType.Forbidden", "You do not have permission to view leave types."));

        var leaveType = await dbContext.LeaveTypes
            .Where(l => l.Id == query.Id)
            .Select(l => new LeaveTypeResponse(l.Id, l.Code, l.Name, l.AccrualFrequency, l.AccrualAmount, l.MaxCarryOverDays, l.IsPaid, l.RequiresDocumentation, l.Status, l.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (leaveType is null)
            return Result.Failure<LeaveTypeResponse>(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{query.Id}' was not found."));

        return Result.Success(leaveType);
    }
}
