namespace ZARI.Application.Features.Loan.LoanApplications.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> APPROVED. No stock/GL side effects, same shape as
/// ApprovePurchaseRequestCommandHandler — creating a LoanAccount off an approved application is a
/// separate manual step (build-order step 4), not an automatic cascade here.
/// </summary>
public sealed class ApproveLoanApplicationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveLoanApplicationCommand, Result<LoanApplicationResponse>>
{
    public async Task<Result<LoanApplicationResponse>> HandleAsync(ApproveLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (application is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Approve, application.BranchId, cancellationToken))
            return Result.Failure<LoanApplicationResponse>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to approve this loan application for this branch."));

        if (application.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.NotPendingApproval", "Only loan applications pending approval can be approved."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_APPLICATION" && r.EntityId == application.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan application."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(decideResult.Error!);

        application.Status = "APPROVED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_APPLICATION", application.Id.ToString(), application.BranchId, "APPROVED", "ACTIVITY",
                "approved this loan application", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(notifyResult.Error!);

        return Result.Success(LoanApplicationMapper.ToResponse(application));
    }
}
