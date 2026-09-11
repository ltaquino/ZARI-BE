namespace ZARI.Application.Features.Loan.LoanPayments.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted, and every schedule-line allocation stands untouched.</summary>
public sealed class RejectLoanPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectLoanPaymentCancellationCommand, Result<LoanPaymentResponse>>
{
    public async Task<Result<LoanPaymentResponse>> HandleAsync(RejectLoanPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.LoanPayments
            .Include(p => p.LoanAccount).ThenInclude(a => a.Customer)
            .Include(p => p.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CostCenter)
            .Include(p => p.Allocations).ThenInclude(a => a.LoanAmortizationScheduleLine)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("LoanPayment.NotFound", $"Loan payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("LOAN_PAYMENTS", cancellationToken))
            return Result.Failure<LoanPaymentResponse>(Error.Forbidden("LoanPayment.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (payment.Status != "PENDING_CANCELLATION")
            return Result.Failure<LoanPaymentResponse>(Error.Validation("LoanPayment.NotPendingCancellation", "Only a loan payment pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "LOAN_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<LoanPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this loan payment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(decideResult.Error!);

        payment.Status = "POSTED";
        payment.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanPaymentResponse>(notifyResult.Error!);

        return Result.Success(LoanPaymentMapper.ToResponse(payment));
    }
}
