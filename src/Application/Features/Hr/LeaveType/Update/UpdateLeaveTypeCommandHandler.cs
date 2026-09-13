namespace ZARI.Application.Features.Hr.LeaveTypes.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateLeaveTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateLeaveTypeCommand>
{
    public async Task<Result> HandleAsync(UpdateLeaveTypeCommand command, CancellationToken cancellationToken = default)
    {
        var leaveType = await dbContext.LeaveTypes.FindAsync([command.Id], cancellationToken);
        if (leaveType is null)
            return Result.Failure(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("LEAVE_TYPES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("LeaveType.Forbidden", "You do not have permission to update leave types."));

        var duplicateCode = await dbContext.LeaveTypes
            .AnyAsync(l => l.Id != command.Id && l.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("LeaveType.DuplicateCode", $"A leave type with code '{command.Code}' already exists."));

        leaveType.Code = command.Code;
        leaveType.Name = command.Name;
        leaveType.AccrualFrequency = command.AccrualFrequency;
        leaveType.AccrualAmount = command.AccrualAmount;
        leaveType.MaxCarryOverDays = command.MaxCarryOverDays;
        leaveType.IsPaid = command.IsPaid;
        leaveType.RequiresDocumentation = command.RequiresDocumentation;
        leaveType.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
