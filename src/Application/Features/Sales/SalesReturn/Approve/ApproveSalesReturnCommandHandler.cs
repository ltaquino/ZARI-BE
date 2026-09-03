namespace ZARI.Application.Features.Sales.SalesReturns.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Mirrors ApproveGoodsReceiptPoCommandHandler's orchestration shape
/// (authoritative re-check, decide the ApprovalRequest, receive stock back in, post GL, flip status)
/// extended with the revenue-side reversal SalesReturnPostingService also performs — the same engine
/// a quick-post Create calls.
/// </summary>
public sealed class ApproveSalesReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesReturnCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(ApproveSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .Include(r => r.Lines).ThenInclude(l => l.DeliveryOrderLine)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Approve, salesReturn.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to approve sales returns for this branch."));

        if (salesReturn.Status != "PENDING_APPROVAL")
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NotPendingApproval", "Only sales returns pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
        // sales return against the same delivery line may have been approved in between. This return
        // is still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from its own
        // "already returned" tally — same pattern as ApproveSalesInvoiceCommandHandler. Run BEFORE
        // deciding the approval request, since DecideApprovalRequestCommand is a one-shot
        // compare-and-swap with no way back.
        if (salesReturn.DeliveryOrderId is not null)
        {
            var deliveryOrder = await dbContext.DeliveryOrders
                .Include(d => d.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(d => d.Id == salesReturn.DeliveryOrderId, cancellationToken);
            if (deliveryOrder is not null)
            {
                var referencedLineIds = salesReturn.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
                var alreadyReturned = await dbContext.SalesReturnLines
                    .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesReturn.Status == "POSTED")
                    .GroupBy(l => l.DeliveryOrderLineId!.Value)
                    .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.QtyReturned) })
                    .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

                var lineInputs = salesReturn.Lines.Select(l => new SalesReturnLineInput(l.ItemId, l.QtyReturned, l.UomId, l.UnitPrice, l.DeliveryOrderLineId, null)).ToList();
                var validationResult = CreateSalesReturnCommandHandler.ValidateAgainstDeliveryOrder(deliveryOrder, lineInputs, alreadyReturned);
                if (!validationResult.IsSuccess)
                    return Result.Failure<SalesReturnResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_RETURN" && r.EntityId == salesReturn.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this sales return."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(decideResult.Error!);

        // No manual VAT overrides here — a normal Submit/Approve flow has nothing to pass (see
        // SalesReturnPostingService's own doc comment for why); any line with no DeliveryOrderLineId
        // falls back to the item's own default VatType.
        var postResult = await SalesReturnPostingService.PostAsync(
            dbContext, receiveStockHandler, reverseIssueSerialHandler, postGlJournalHandler, salesReturn, null, cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(postResult.Error!);

        // ReceiveStockCommand runs its own retryable transaction and calls ChangeTracker.Clear() at
        // the start of every attempt — that detaches the `salesReturn` this handler loaded earlier,
        // so mutating it and calling SaveChangesAsync would silently persist nothing.
        // ExecuteUpdateAsync writes directly, independent of whatever the tracker currently holds.
        salesReturn.Status = "POSTED";
        await dbContext.SalesReturns.Where(r => r.Id == salesReturn.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "APPROVED", "ACTIVITY",
                "approved this sales return", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
