namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED payment has to go through RequestOutgoingPaymentCancellation instead.
/// </summary>
public sealed class CancelOutgoingPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelOutgoingPaymentCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(CancelOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Cancel, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to cancel outgoing payments for this branch."));

        if (payment.Status == "CANCELLED")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.AlreadyCancelled", "This outgoing payment is already cancelled."));

        if (payment.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.RequiresCancellationRequest", "A posted outgoing payment must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("OUTGOING_PAYMENT", payment.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(cancelPendingResult.Error!);

        payment.Status = "CANCELLED";
        payment.CancelledBy = command.CancelledBy;
        payment.CancelledAt = DateTimeOffset.UtcNow;
        payment.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this outgoing payment — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
