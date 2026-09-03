namespace ZARI.Application.Features.Sales.CustomerPayments.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitCustomerPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitCustomerPaymentCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(SubmitCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice)
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Edit, payment.BranchId, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to submit customer payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NotDraft", "Only draft customer payments can be submitted for approval."));

        if (payment.Lines.Count == 0)
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NoLines", "Add at least one invoice before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(submitResult.Error!);

        payment.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this customer payment for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
