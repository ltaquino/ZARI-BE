namespace ZARI.Application.Features.Hr.LeaveRequests.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitLeaveRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitLeaveRequestCommand, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> HandleAsync(SubmitLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.LeaveRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure<LeaveRequestResponse>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to submit this leave request for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure<LeaveRequestResponse>(Error.Validation("LeaveRequest.NotDraft", "Only draft leave requests can be submitted for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LEAVE_REQUEST", request.Id.ToString(), employee.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(submitResult.Error!);

        request.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LEAVE_REQUEST", request.Id.ToString(), employee.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this leave request for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(notifyResult.Error!);

        var response = new LeaveRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.LeaveTypeId, request.DateFrom, request.DateTo, request.Days, request.Reason, request.AttachmentUrl, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
