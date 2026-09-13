namespace ZARI.Application.Features.Hr.PayrollRuns.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Requires at least one computed Line — an empty run can't be submitted.</summary>
public sealed class SubmitPayrollRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitPayrollRunCommand, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(SubmitPayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.Employee)
            .Include(r => r.Lines).ThenInclude(l => l.LeaveTypeConsumed)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (run is null)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to submit payroll runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure<PayrollRunResponse>(Error.Validation("PayrollRun.NotDraft", "Only a draft payroll run can be submitted for approval."));

        if (run.Lines.Count == 0)
            return Result.Failure<PayrollRunResponse>(Error.Validation("PayrollRun.NoLines", "Run Compute at least once before submitting — this payroll run has no lines yet."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("PAYROLL_RUN", run.Id.ToString(), run.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(submitResult.Error!);

        run.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PAYROLL_RUN", run.Id.ToString(), run.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this payroll run for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(notifyResult.Error!);

        return Result.Success(PayrollRunMapper.ToResponse(run));
    }
}
