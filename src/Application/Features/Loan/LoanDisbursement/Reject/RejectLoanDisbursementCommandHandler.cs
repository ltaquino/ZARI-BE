namespace ZARI.Application.Features.Loan.LoanDisbursements.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectLoanDisbursementCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanDisbursementCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(RejectLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Approve, disbursement.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to reject loan disbursements for this branch."));

        if (disbursement.Status != "PENDING_APPROVAL")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotPendingApproval", "Only a loan disbursement pending approval can be rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_DISBURSEMENT" && r.EntityId == disbursement.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this loan disbursement."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(decideResult.Error!);

        disbursement.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this loan disbursement — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
