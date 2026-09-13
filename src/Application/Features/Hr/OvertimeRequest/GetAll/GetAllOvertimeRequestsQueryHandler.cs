namespace ZARI.Application.Features.Hr.OvertimeRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Domain.Common;

public sealed class GetAllOvertimeRequestsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllOvertimeRequestsQuery, Result<List<OvertimeRequestResponse>>>
{
    public async Task<Result<List<OvertimeRequestResponse>>> HandleAsync(GetAllOvertimeRequestsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("OVERTIME_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<List<OvertimeRequestResponse>>(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to view overtime requests."));

        var items = await dbContext.OvertimeRequests.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OvertimeRequestResponse(o.Id, o.RequestNo, o.EmployeeId, o.Date, o.PlannedTimeFrom, o.PlannedTimeTo, o.OtType, o.Reason, o.Status, o.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
