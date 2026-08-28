namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Submit;

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

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitOutgoingPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitOutgoingPaymentCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(SubmitOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Edit, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to submit outgoing payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NotDraft", "Only draft outgoing payments can be submitted for approval."));

        if (payment.Lines.Count == 0)
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NoLines", "Add at least one invoice before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(submitResult.Error!);

        payment.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this outgoing payment for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
