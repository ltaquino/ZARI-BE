namespace ZARI.Application.Features.Hr.LeaveRequests.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectLeaveRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLeaveRequestCommand, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> HandleAsync(RejectLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.LeaveRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Approve, employee!.BranchId, cancellationToken))
            return Result.Failure<LeaveRequestResponse>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to reject this leave request for this branch."));

        if (request.Status != "PENDING_APPROVAL")
            return Result.Failure<LeaveRequestResponse>(Error.Validation("LeaveRequest.NotPendingApproval", "Only leave requests pending approval can be rejected."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LEAVE_REQUEST" && r.EntityId == request.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this leave request."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(decideResult.Error!);

        request.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LEAVE_REQUEST", request.Id.ToString(), employee.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this leave request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(notifyResult.Error!);

        var response = new LeaveRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.LeaveTypeId, request.DateFrom, request.DateTo, request.Days, request.Reason, request.AttachmentUrl, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
