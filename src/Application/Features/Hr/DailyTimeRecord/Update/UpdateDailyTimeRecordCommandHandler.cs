namespace ZARI.Application.Features.Hr.DailyTimeRecords.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateDailyTimeRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateDailyTimeRecordCommand>
{
    public async Task<Result> HandleAsync(UpdateDailyTimeRecordCommand command, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.DailyTimeRecords.FindAsync([command.Id], cancellationToken);
        if (record is null)
            return Result.Failure(Error.NotFound("DailyTimeRecord.NotFound", $"Daily time record with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([record.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DailyTimeRecord.Forbidden", "You do not have permission to update daily time records for this branch."));

        record.TimeIn = command.TimeIn;
        record.TimeOut = command.TimeOut;
        record.BreakOutTime = command.BreakOutTime;
        record.BreakInTime = command.BreakInTime;
        record.Source = command.Source;
        record.Remarks = command.Remarks;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
