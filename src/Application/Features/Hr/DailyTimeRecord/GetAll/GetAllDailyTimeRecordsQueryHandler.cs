namespace ZARI.Application.Features.Hr.DailyTimeRecords.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Domain.Common;

public sealed class GetAllDailyTimeRecordsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllDailyTimeRecordsQuery, Result<List<DailyTimeRecordResponse>>>
{
    public async Task<Result<List<DailyTimeRecordResponse>>> HandleAsync(GetAllDailyTimeRecordsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DAILY_TIME_RECORDS", FormAction.View, cancellationToken))
            return Result.Failure<List<DailyTimeRecordResponse>>(Error.Forbidden("DailyTimeRecord.Forbidden", "You do not have permission to view daily time records."));

        var rows = await dbContext.DailyTimeRecords.AsNoTracking()
            .OrderByDescending(d => d.Date)
            .Select(d => new { d.Id, d.EmployeeId, d.Date, d.TimeIn, d.TimeOut, d.BreakOutTime, d.BreakInTime, d.Source, d.Remarks, d.CreatedAt })
            .ToListAsync(cancellationToken);

        var items = new List<DailyTimeRecordResponse>(rows.Count);
        foreach (var row in rows)
        {
            var computed = await DtrComputationService.ComputeAsync(dbContext, row.EmployeeId, row.Date, row.TimeIn, row.TimeOut, cancellationToken);
            items.Add(new DailyTimeRecordResponse(row.Id, row.EmployeeId, row.Date, row.TimeIn, row.TimeOut, row.BreakOutTime, row.BreakInTime,
                row.Source, row.Remarks, computed.LateMinutes, computed.UndertimeMinutes, computed.IsAbsent, row.CreatedAt));
        }

        return Result.Success(items);
    }
}
