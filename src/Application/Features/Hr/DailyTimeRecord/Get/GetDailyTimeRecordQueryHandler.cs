namespace ZARI.Application.Features.Hr.DailyTimeRecords.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Domain.Common;

public sealed class GetDailyTimeRecordQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetDailyTimeRecordQuery, Result<DailyTimeRecordResponse>>
{
    public async Task<Result<DailyTimeRecordResponse>> HandleAsync(GetDailyTimeRecordQuery query, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.DailyTimeRecords
            .Where(d => d.Id == query.Id)
            .Select(d => new { d.Id, d.EmployeeId, d.Date, d.TimeIn, d.TimeOut, d.BreakOutTime, d.BreakInTime, d.Source, d.Remarks, d.CreatedAt, d.Employee.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result.Failure<DailyTimeRecordResponse>(Error.NotFound("DailyTimeRecord.NotFound", $"Daily time record with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.View, row.BranchId, cancellationToken))
            return Result.Failure<DailyTimeRecordResponse>(Error.Forbidden("DailyTimeRecord.Forbidden", "You do not have permission to view daily time records for this branch."));

        var computed = await DtrComputationService.ComputeAsync(dbContext, row.EmployeeId, row.Date, row.TimeIn, row.TimeOut, cancellationToken);

        var response = new DailyTimeRecordResponse(row.Id, row.EmployeeId, row.Date, row.TimeIn, row.TimeOut, row.BreakOutTime, row.BreakInTime,
            row.Source, row.Remarks, computed.LateMinutes, computed.UndertimeMinutes, computed.IsAbsent, row.CreatedAt);
        return Result.Success(response);
    }
}
