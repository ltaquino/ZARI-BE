namespace ZARI.Application.Features.Sales.CustomerPayments.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Reverses the posted GL journal and re-derives every
/// referenced Sales Invoice's status from its remaining balance with this payment's contribution
/// backed out — back to POSTED if nothing else is paid, or down to PARTIALLY_PAID if some other
/// payment still covers part of it. No stock reversal — a payment never moved stock in the first
/// place. Mirrors ApproveOutgoingPaymentCancellationCommandHandler exactly, AR-side.
/// </summary>
public sealed class ApproveCustomerPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveCustomerPaymentCancellationCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(ApproveCustomerPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice).ThenInclude(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("CUSTOMER_PAYMENTS", cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (payment.Status != "PENDING_CANCELLATION")
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NotPendingCancellation", "Only a customer payment pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "CUSTOMER_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this customer payment."));

        // Decide before any reversal side-effect — see ApproveGoodsReceiptCancellationCommandHandler's
        // doc comment for why (a failed decide must leave nothing reversed yet, so it stays retryable).
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(decideResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("CustomerPayment", payment.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {payment.PaymentNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(reverseJournalsResult.Error!);

        payment.Status = "CANCELLED";
        payment.CancelledBy = command.ApproverUserId;
        payment.CancelledAt = DateTimeOffset.UtcNow;
        // This payment is PENDING_CANCELLATION (not POSTED) right now, so GetAmountPaidAsync
        // naturally excludes its own contribution — what's left is whatever other payments still cover.
        foreach (var line in payment.Lines)
        {
            var invoice = line.SalesInvoice;
            var invoiceTotal = SalesInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaidByOthers = await SalesInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            invoice.Status = SalesInvoicePaymentBalance.DetermineStatus(invoiceTotal, amountPaidByOthers);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
