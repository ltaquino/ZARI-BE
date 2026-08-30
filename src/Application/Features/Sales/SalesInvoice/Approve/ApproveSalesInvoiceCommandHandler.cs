namespace ZARI.Application.Features.Sales.SalesInvoices.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Like ApInvoice, nothing here runs its own retryable transaction (no
/// stock engine involved), so a plain SaveChangesAsync at the end is enough. Assigns the BIR-OR
/// number and posts the AR/Revenue/VAT journal via SalesInvoicePostingService — the same engine a
/// quick-post Create calls.
/// </summary>
public sealed class ApproveSalesInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesInvoiceCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(ApproveSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Approve, invoice.BranchId, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to approve sales invoices for this branch."));

        if (invoice.Status != "PENDING_APPROVAL")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NotPendingApproval", "Only sales invoices pending approval can be approved."));

        // Authoritative re-check, run BEFORE deciding the approval request — same ordering as
        // ApproveApInvoiceCommandHandler, for the same reason: DecideApprovalRequestCommand is a
        // one-shot compare-and-swap with no way back, so failing a check AFTER deciding would leave
        // the document stuck approved-but-not-POSTED with no path to approve/reject/cancel it.
        if (invoice.DeliveryOrderId is not null)
        {
            var deliveryOrder = await dbContext.DeliveryOrders
                .Include(d => d.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(d => d.Id == invoice.DeliveryOrderId, cancellationToken);
            if (deliveryOrder is not null)
            {
                var referencedLineIds = invoice.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
                var alreadyInvoiced = await dbContext.SalesInvoiceLines
                    .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesInvoice.Status == "POSTED")
                    .GroupBy(l => l.DeliveryOrderLineId!.Value)
                    .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                    .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

                var lineInputs = invoice.Lines.Select(l => new SalesInvoiceLineInput(
                    l.ItemId, l.Qty, l.UomId, l.UnitPrice, l.DiscountPct, l.DiscountSourceType, l.DiscountSourceId,
                    l.VatType, l.StatutoryDiscountTypeId, l.StatutoryIdNumber, l.DeliveryOrderLineId)).ToList();
                var validationResult = CreateSalesInvoiceCommandHandler.ValidateAgainstDeliveryOrder(deliveryOrder, lineInputs, alreadyInvoiced);
                if (!validationResult.IsSuccess)
                    return Result.Failure<SalesInvoiceResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_INVOICE" && r.EntityId == invoice.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this sales invoice."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(decideResult.Error!);

        var postResult = await SalesInvoicePostingService.PostAsync(dbContext, nextDocumentNumberHandler, postGlJournalHandler, invoice, cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(postResult.Error!);

        invoice.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "APPROVED", "ACTIVITY",
                "approved this sales invoice", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
