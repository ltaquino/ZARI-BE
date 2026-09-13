namespace ZARI.Application.Features.Loan.LoanDisbursements.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED disbursement has to go through RequestLoanDisbursementCancellation instead.
/// </summary>
public sealed class CancelLoanDisbursementCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelLoanDisbursementCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(CancelLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Cancel, disbursement.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to cancel this loan disbursement for this branch."));

        if (disbursement.Status == "CANCELLED")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.AlreadyCancelled", "This loan disbursement is already cancelled."));

        if (disbursement.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.RequiresCancellationRequest", "A posted loan disbursement must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(cancelPendingResult.Error!);

        disbursement.Status = "CANCELLED";
        disbursement.CancelledBy = command.CancelledBy;
        disbursement.CancelledAt = DateTimeOffset.UtcNow;
        disbursement.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this loan disbursement — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(disbursement));
    }
}
