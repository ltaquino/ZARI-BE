namespace ZARI.Application.Features.Hr.LeaveTypes.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLeaveTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateLeaveTypeCommand, Result<LeaveTypeResponse>>
{
    public async Task<Result<LeaveTypeResponse>> HandleAsync(CreateLeaveTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LEAVE_TYPES", FormAction.Create, cancellationToken))
            return Result.Failure<LeaveTypeResponse>(Error.Forbidden("LeaveType.Forbidden", "You do not have permission to create leave types."));

        var codeExists = await dbContext.LeaveTypes.AnyAsync(l => l.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<LeaveTypeResponse>(Error.Conflict("LeaveType.DuplicateCode", $"A leave type with code '{command.Code}' already exists."));

        var leaveType = new LeaveType
        {
            Code = command.Code,
            Name = command.Name,
            AccrualFrequency = command.AccrualFrequency,
            AccrualAmount = command.AccrualAmount,
            MaxCarryOverDays = command.MaxCarryOverDays,
            IsPaid = command.IsPaid,
            RequiresDocumentation = command.RequiresDocumentation,
            Status = command.Status
        };

        dbContext.LeaveTypes.Add(leaveType);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new LeaveTypeResponse(leaveType.Id, leaveType.Code, leaveType.Name, leaveType.AccrualFrequency, leaveType.AccrualAmount, leaveType.MaxCarryOverDays, leaveType.IsPaid, leaveType.RequiresDocumentation, leaveType.Status, leaveType.CreatedAt);
        return Result.Success(response);
    }
}
