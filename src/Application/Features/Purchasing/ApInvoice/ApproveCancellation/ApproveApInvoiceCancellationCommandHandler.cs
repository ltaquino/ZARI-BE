namespace ZARI.Application.Features.Purchasing.ApInvoices.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. No stock reversal — an AP invoice never moved stock in the first place — just reverse
/// the posted GL journal(s), then decide the cancellation request. A plain SaveChangesAsync is
/// enough here too — no stock engine call runs its own retryable transaction to detach the tracker.
/// </summary>
public sealed class ApproveApInvoiceCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveApInvoiceCancellationCommand, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(ApproveApInvoiceCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("AP_INVOICES", cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (invoice.Status != "PENDING_CANCELLATION")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NotPendingCancellation", "Only an AP invoice pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "AP_INVOICE" && r.EntityId == invoice.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this AP invoice."));

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("ApInvoice", invoice.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {invoice.InvoiceNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(decideResult.Error!);

        invoice.Status = "CANCELLED";
        invoice.CancelledBy = command.ApproverUserId;
        invoice.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }
}
