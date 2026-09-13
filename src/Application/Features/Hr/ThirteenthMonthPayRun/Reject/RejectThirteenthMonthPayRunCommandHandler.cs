namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so HR can fix the issue (recompute if needed) and resubmit.</summary>
public sealed class RejectThirteenthMonthPayRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(RejectThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Approve, run.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to reject this 13th month pay run for this branch."));

        if (run.Status != "PENDING_APPROVAL")
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Validation("ThirteenthMonthPayRun.NotPendingApproval", "Only a 13th month pay run pending approval can be rejected."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "THIRTEENTH_MONTH_PAY_RUN" && r.EntityId == run.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this 13th month pay run."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(decideResult.Error!);

        run.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("THIRTEENTH_MONTH_PAY_RUN", run.Id.ToString(), run.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this 13th month pay run — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(notifyResult.Error!);

        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(run));
    }
}
