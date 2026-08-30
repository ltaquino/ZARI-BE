namespace ZARI.Application.Features.Sales.SalesInvoices.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal. No stock reversal
/// — a Sales Invoice never touched stock in the first place, Delivery owns that independently — just
/// reverse the posted GL journal, then decide the cancellation request. The BIR-OR number stays on
/// the record even after cancellation (a voided receipt number is never reused, standard BIR
/// practice) — nothing here clears BirOrSeriesNumber. A plain SaveChangesAsync is enough, same as
/// ApproveApInvoiceCancellationCommandHandler.
/// </summary>
public sealed class ApproveSalesInvoiceCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesInvoiceCancellationCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(ApproveSalesInvoiceCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("SALES_INVOICES", cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (invoice.Status != "PENDING_CANCELLATION")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NotPendingCancellation", "Only a sales invoice pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_INVOICE" && r.EntityId == invoice.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this sales invoice."));

        // Decide before any reversal side-effect — see ApproveGoodsReceiptCancellationCommandHandler's
        // doc comment for why (a failed decide must leave nothing reversed yet, so it stays retryable).
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(decideResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("SalesInvoice", invoice.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {invoice.InvoiceNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(reverseJournalsResult.Error!);

        invoice.Status = "CANCELLED";
        invoice.CancelledBy = command.ApproverUserId;
        invoice.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
