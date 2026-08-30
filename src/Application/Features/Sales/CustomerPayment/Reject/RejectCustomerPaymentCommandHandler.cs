namespace ZARI.Application.Features.Sales.CustomerPayments.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectCustomerPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectCustomerPaymentCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(RejectCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Approve, payment.BranchId, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to reject customer payments for this branch."));

        if (payment.Status != "PENDING_APPROVAL")
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NotPendingApproval", "Only customer payments pending approval can be rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "CUSTOMER_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this customer payment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(decideResult.Error!);

        payment.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this customer payment — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
