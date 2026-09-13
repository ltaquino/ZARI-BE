namespace ZARI.Application.Features.Hr.LeaveTypes.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLeaveTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteLeaveTypeCommand>
{
    public async Task<Result> HandleAsync(DeleteLeaveTypeCommand command, CancellationToken cancellationToken = default)
    {
        var leaveType = await dbContext.LeaveTypes.FindAsync([command.Id], cancellationToken);
        if (leaveType is null)
            return Result.Failure(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("LEAVE_TYPES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("LeaveType.Forbidden", "You do not have permission to delete leave types."));

        var requestCount = await dbContext.LeaveRequests.CountAsync(r => r.LeaveTypeId == command.Id, cancellationToken);
        if (requestCount > 0)
            return Result.Failure(Error.Conflict("LeaveType.HasRequests", $"Cannot delete this leave type — it has {requestCount} leave request{(requestCount == 1 ? "" : "s")}."));

        var entryCount = await dbContext.LeaveBalanceEntries.CountAsync(e => e.LeaveTypeId == command.Id, cancellationToken);
        if (entryCount > 0)
            return Result.Failure(Error.Conflict("LeaveType.HasBalanceEntries", $"Cannot delete this leave type — it has {entryCount} balance ledger entr{(entryCount == 1 ? "y" : "ies")}."));

        dbContext.LeaveTypes.Remove(leaveType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
