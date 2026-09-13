namespace ZARI.Application.Features.Hr.DailyTimeRecords.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDailyTimeRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateDailyTimeRecordCommand, Result<DailyTimeRecordResponse>>
{
    public async Task<Result<DailyTimeRecordResponse>> HandleAsync(CreateDailyTimeRecordCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FindAsync([command.EmployeeId], cancellationToken);
        if (employee is null)
            return Result.Failure<DailyTimeRecordResponse>(Error.NotFound("Employee.NotFound", $"Employee with ID '{command.EmployeeId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Create, employee.BranchId, cancellationToken))
            return Result.Failure<DailyTimeRecordResponse>(Error.Forbidden("DailyTimeRecord.Forbidden", "You do not have permission to create daily time records for this branch."));

        var recordDate = command.Date.Date;
        var alreadyExists = await dbContext.DailyTimeRecords
            .AnyAsync(d => d.EmployeeId == command.EmployeeId && d.Date.Date == recordDate, cancellationToken);
        if (alreadyExists)
            return Result.Failure<DailyTimeRecordResponse>(Error.Conflict("DailyTimeRecord.AlreadyExists", "A daily time record for this employee on this date already exists — update it instead."));

        var record = new DailyTimeRecord
        {
            EmployeeId = command.EmployeeId,
            Date = command.Date,
            TimeIn = command.TimeIn,
            TimeOut = command.TimeOut,
            BreakOutTime = command.BreakOutTime,
            BreakInTime = command.BreakInTime,
            Source = command.Source,
            Remarks = command.Remarks
        };

        dbContext.DailyTimeRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        var computed = await DtrComputationService.ComputeAsync(dbContext, record.EmployeeId, record.Date, record.TimeIn, record.TimeOut, cancellationToken);
        var response = new DailyTimeRecordResponse(record.Id, record.EmployeeId, record.Date, record.TimeIn, record.TimeOut, record.BreakOutTime, record.BreakInTime,
            record.Source, record.Remarks, computed.LateMinutes, computed.UndertimeMinutes, computed.IsAbsent, record.CreatedAt);
        return Result.Success(response);
    }
}
