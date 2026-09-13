namespace ZARI.Application.Features.Hr.PayrollRuns.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so HR can fix the issue (recompute if needed) and resubmit.</summary>
public sealed class RejectPayrollRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectPayrollRunCommand, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(RejectPayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Approve, run.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to reject this payroll run for this branch."));

        if (run.Status != "PENDING_APPROVAL")
            return Result.Failure<PayrollRunResponse>(Error.Validation("PayrollRun.NotPendingApproval", "Only a payroll run pending approval can be rejected."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "PAYROLL_RUN" && r.EntityId == run.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this payroll run."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(decideResult.Error!);

        run.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PAYROLL_RUN", run.Id.ToString(), run.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this payroll run — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(notifyResult.Error!);

        return Result.Success(PayrollRunMapper.ToResponse(run));
    }
}
