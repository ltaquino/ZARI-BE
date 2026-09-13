namespace ZARI.Application.Features.Hr.OvertimeRequests.Submit;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitOvertimeRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitOvertimeRequestCommand, Result<OvertimeRequestResponse>>
{
    public async Task<Result<OvertimeRequestResponse>> HandleAsync(SubmitOvertimeRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.OvertimeRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure<OvertimeRequestResponse>(Error.NotFound("OvertimeRequest.NotFound", $"Overtime request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Edit, employee!.BranchId, cancellationToken))
            return Result.Failure<OvertimeRequestResponse>(Error.Forbidden("OvertimeRequest.Forbidden", "You do not have permission to submit this overtime request for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure<OvertimeRequestResponse>(Error.Validation("OvertimeRequest.NotDraft", "Only draft overtime requests can be submitted for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("OVERTIME_REQUEST", request.Id.ToString(), employee.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<OvertimeRequestResponse>(submitResult.Error!);

        request.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OVERTIME_REQUEST", request.Id.ToString(), employee.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this overtime request for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OvertimeRequestResponse>(notifyResult.Error!);

        var response = new OvertimeRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, request.OtType, request.Reason, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
