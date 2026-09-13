namespace ZARI.Application.Features.Hr.DailyTimeRecords.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDailyTimeRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteDailyTimeRecordCommand>
{
    public async Task<Result> HandleAsync(DeleteDailyTimeRecordCommand command, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.DailyTimeRecords.FindAsync([command.Id], cancellationToken);
        if (record is null)
            return Result.Failure(Error.NotFound("DailyTimeRecord.NotFound", $"Daily time record with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([record.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Delete, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DailyTimeRecord.Forbidden", "You do not have permission to delete daily time records for this branch."));

        dbContext.DailyTimeRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
