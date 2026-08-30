namespace ZARI.Application.Features.Sales.CustomerPayments.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. No stock movement — a payment never touches inventory. Converts
/// "1200" Accounts Receivable into an actual cash/bank inflow (Dr the selected cash/bank GL
/// account, Cr Accounts Receivable), then moves every referenced Sales Invoice to PARTIALLY_PAID
/// or PAID depending on how much of its balance this payment actually covers. Each invoice's
/// eligibility and remaining balance are re-checked here (not just at Create/Update) to close the
/// race where two payments both draw against the same invoice's balance — whichever approves first
/// claims that part of the balance. Mirrors ApproveOutgoingPaymentCommandHandler exactly, AR-side.
/// </summary>
public sealed class ApproveCustomerPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveCustomerPaymentCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(ApproveCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.CashAccount)
            .Include(p => p.Lines).ThenInclude(l => l.SalesInvoice).ThenInclude(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Approve, payment.BranchId, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to approve customer payments for this branch."));

        if (payment.Status != "PENDING_APPROVAL")
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NotPendingApproval", "Only customer payments pending approval can be approved."));

        // Authoritative re-check, run BEFORE deciding the approval request — DecideApprovalRequestCommand
        // is a one-shot compare-and-swap with no way back, so a failure discovered after deciding would
        // leave the document stuck approved-but-not-POSTED with no path to approve/reject/cancel it.
        var statusesResult = await CustomerPaymentPostingService.ComputeNewInvoiceStatusesAsync(dbContext, payment, cancellationToken);
        if (!statusesResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(statusesResult.Error!);
        var newInvoiceStatuses = statusesResult.Value!;

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "CUSTOMER_PAYMENT" && r.EntityId == payment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this customer payment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(decideResult.Error!);

        var journalResult = await CustomerPaymentPostingService.PostPaymentJournalAsync(dbContext, postGlJournalHandler, payment, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(journalResult.Error!);

        payment.Status = "POSTED";
        foreach (var line in payment.Lines)
            line.SalesInvoice.Status = newInvoiceStatuses[line.SalesInvoice.Id];
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "APPROVED", "ACTIVITY",
                "approved this customer payment", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
