namespace ZARI.Application.Features.Hr.LeaveRequests.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> APPROVED, then posts a USAGE row (negative Days) to LeaveBalanceEntry via
/// LeaveBalanceLedgerService — the only place USAGE entries are ever written. Deliberately does
/// NOT block approval when the resulting balance would go negative: whether unpaid/over-balance
/// leave should be allowed at all is a real HR policy question flagged OPEN in
/// HrModuleContext.md §6 item 10, not invented here — the ledger simply records whatever balance
/// results, negative or not, for HR to see and act on.
/// </summary>
public sealed class ApproveLeaveRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLeaveRequestCommand, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> HandleAsync(ApproveLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.LeaveRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("LeaveRequest.NotFound", $"Leave request with ID '{command.Id}' was not found."));

        var employee = await dbContext.Employees.FindAsync([request.EmployeeId], cancellationToken);
        if (!await permissionService.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Approve, employee!.BranchId, cancellationToken))
            return Result.Failure<LeaveRequestResponse>(Error.Forbidden("LeaveRequest.Forbidden", "You do not have permission to approve this leave request for this branch."));

        if (request.Status != "PENDING_APPROVAL")
            return Result.Failure<LeaveRequestResponse>(Error.Validation("LeaveRequest.NotPendingApproval", "Only leave requests pending approval can be approved."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LEAVE_REQUEST" && r.EntityId == request.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<LeaveRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this leave request."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(decideResult.Error!);

        request.Status = "APPROVED";
        await LeaveBalanceLedgerService.PostEntryAsync(
            dbContext, request.EmployeeId, request.LeaveTypeId, request.DateFrom, "USAGE", -request.Days,
            "LeaveRequests", request.Id.ToString(), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LEAVE_REQUEST", request.Id.ToString(), employee.BranchId, "APPROVED", "ACTIVITY",
                "approved this leave request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LeaveRequestResponse>(notifyResult.Error!);

        var response = new LeaveRequestResponse(request.Id, request.RequestNo, request.EmployeeId, request.LeaveTypeId, request.DateFrom, request.DateTo, request.Days, request.Reason, request.AttachmentUrl, request.Status, request.CreatedAt);
        return Result.Success(response);
    }
}
