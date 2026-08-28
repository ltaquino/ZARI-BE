namespace ZARI.Application.Features.Purchasing.OutgoingPayments.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestOutgoingPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestOutgoingPaymentCancellationCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(RequestOutgoingPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Cancel, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to request cancellation of outgoing payments for this branch."));

        if (payment.Status != "POSTED")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NotPosted", "Only a posted outgoing payment can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(submitResult.Error!);

        payment.Status = "PENDING_CANCELLATION";
        payment.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
