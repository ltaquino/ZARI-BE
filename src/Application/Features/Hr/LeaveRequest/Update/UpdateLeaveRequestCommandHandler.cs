namespace ZARI.Application.Features.Hr.LeaveRequests.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Shared;
using ZARI.Domain.Common;

/// <summary>Only DRAFT requests can be edited — matches PurchaseRequest's convention.</summary>
public sealed class UpdateLeaveRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateLeaveRequestCommand>
{
    public async Task<Result> HandleAsync(UpdateLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.LeaveRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to update leave requests for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("LeaveRequest.NotDraft", "Only draft leave requests can be edited."));

        var leaveTypeExists = await dbContext.LeaveTypes.AnyAsync(l => l.Id == command.LeaveTypeId, cancellationToken);
        if (!leaveTypeExists)
            return Result.Failure(Error.NotFound("LeaveType.NotFound", $"Leave type with ID '{command.LeaveTypeId}' was not found."));

        request.LeaveTypeId = command.LeaveTypeId;
        request.DateFrom = command.DateFrom;
        request.DateTo = command.DateTo;
        request.Days = await LeaveRequestDaysCalculator.CalculateAsync(dbContext, request.EmployeeId, command.DateFrom, command.DateTo, cancellationToken);
        request.Reason = command.Reason;
        request.AttachmentUrl = command.AttachmentUrl;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
