namespace ZARI.Application.Features.Purchasing.OutgoingPayments.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Reverses the posted GL journal and re-derives every
/// referenced AP Invoice's status from its remaining balance with this payment's contribution
/// backed out — back to POSTED if nothing else is paid, or down to PARTIALLY_PAID if some other
/// payment still covers part of it. No stock reversal — a payment never moved stock in the first place.
/// </summary>
public sealed class ApproveOutgoingPaymentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveOutgoingPaymentCancellationCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(ApproveOutgoingPaymentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice).ThenInclude(i => i.Lines)
            .Include(p => p.Lines).ThenInclude(l => l.ApInvoice).ThenInclude(i => i.ExpenseLines)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("OUTGOING_PAYMENTS", cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (payment.Status != "PENDING_CANCELLATION")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NotPendingCancellation", "Only an outgoing payment pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "OUTGOING_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this outgoing payment."));

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("OutgoingPayment", payment.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {payment.PaymentNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(decideResult.Error!);

        payment.Status = "CANCELLED";
        payment.CancelledBy = command.ApproverUserId;
        payment.CancelledAt = DateTimeOffset.UtcNow;
        // This payment is PENDING_CANCELLATION (not POSTED) right now, so GetAmountPaidAsync
        // naturally excludes its own contribution — what's left is whatever other payments still cover.
        foreach (var line in payment.Lines)
        {
            var invoice = line.ApInvoice;
            var invoiceTotal = ApInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaidByOthers = await ApInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            invoice.Status = ApInvoicePaymentBalance.DetermineStatus(invoiceTotal, amountPaidByOthers);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
