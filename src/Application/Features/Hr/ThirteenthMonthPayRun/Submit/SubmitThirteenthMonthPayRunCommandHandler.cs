namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Requires at least one computed Line — an empty run can't be submitted.</summary>
public sealed class SubmitThirteenthMonthPayRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(SubmitThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to submit 13th month pay runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Validation("ThirteenthMonthPayRun.NotDraft", "Only a draft 13th month pay run can be submitted for approval."));

        if (run.Lines.Count == 0)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Validation("ThirteenthMonthPayRun.NoLines", "Run Compute at least once before submitting — this 13th month pay run has no lines yet."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("THIRTEENTH_MONTH_PAY_RUN", run.Id.ToString(), run.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(submitResult.Error!);

        run.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("THIRTEENTH_MONTH_PAY_RUN", run.Id.ToString(), run.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this 13th month pay run for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(notifyResult.Error!);

        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(run));
    }
}
