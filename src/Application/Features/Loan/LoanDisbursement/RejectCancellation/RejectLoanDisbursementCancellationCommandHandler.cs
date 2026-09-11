namespace ZARI.Application.Features.Loan.LoanDisbursements.RejectCancellation;

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

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted, and the loan account stays ACTIVE.</summary>
public sealed class RejectLoanDisbursementCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(RejectLoanDisbursementCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_DISBURSEMENTS", cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (disbursement.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotPendingCancellation", "Only a loan disbursement pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_DISBURSEMENT" && r.EntityId == disbursement.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan disbursement."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(decideResult.Error!);

        disbursement.Status = "POSTED";
        disbursement.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
