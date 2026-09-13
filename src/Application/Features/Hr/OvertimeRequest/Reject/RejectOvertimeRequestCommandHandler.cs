namespace ZARI.Application.Features.Hr.OvertimeRequests.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectOvertimeRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectOvertimeRequestCommand, Result<OvertimeRequestResponse>>
{
    public async Task<Result<OvertimeRequestResponse>> HandleAsync(RejectOvertimeRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.OvertimeRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure<OvertimeRequestResponse>(Error.NotFound("OvertimeRequest.NotFound", $"Overtime request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Approve, employee!.BranchId, cancellationToken))
            return Result.Failure<OvertimeRequestResponse>(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to reject this overtime request for this branch."));

        if (request.Status != "PENDING_APPROVAL")
            return Result.Failure<OvertimeRequestResponse>(Error.Validation("OvertimeRequest.NotPendingApproval", "Only overtime requests pending approval can be rejected."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "OVERTIME_REQUEST" && r.EntityId == request.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<OvertimeRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this overtime request."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<OvertimeRequestResponse>(decideResult.Error!);

        request.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OVERTIME_REQUEST", request.Id.ToString(), employee.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this overtime request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OvertimeRequestResponse>(notifyResult.Error!);

        var response = new OvertimeRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, request.OtType, request.Reason, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
