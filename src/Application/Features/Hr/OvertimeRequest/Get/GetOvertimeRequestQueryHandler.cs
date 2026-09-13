namespace ZARI.Application.Features.Hr.OvertimeRequests.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetOvertimeRequestQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetOvertimeRequestQuery, Result<OvertimeRequestResponse>>
{
    public async Task<Result<OvertimeRequestResponse>> HandleAsync(GetOvertimeRequestQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.OvertimeRequests
            .Where(o => o.Id == query.Id)
            .Select(o => new { o.Id, o.RequestNo, o.EmployeeId, o.Date, o.PlannedTimeFrom, o.PlannedTimeTo, o.OtType, o.Reason, o.Status, o.CreatedAt, o.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<OvertimeRequestResponse>(Error.NotFound("OvertimeRequest.NotFound", $"Overtime request with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<OvertimeRequestResponse>(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to view overtime requests for this branch."));

        var response = new OvertimeRequestResponse(row.Id, row.RequestNo, row.EmployeeId, row.Date, row.PlannedTimeFrom, row.PlannedTimeTo, row.OtType, row.Reason, row.Status, row.CreatedAt);
        return Result.Success(response);
    }
}
