namespace ZARI.Application.Features.Sales.DeliveryOrders.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Mirrors ApproveGoodsIssueCommandHandler's orchestration via
/// DeliveryPostingService (the same engine a quick-post Create calls) — issue stock, decide the
/// approval request, then post the COGS/Inventory GL journal.
/// </summary>
public sealed class ApproveDeliveryOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(ApproveDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Approve, order.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to approve deliveries for this branch."));

        if (order.Status != "PENDING_APPROVAL")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.NotPendingApproval", "Only deliveries pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
        // delivery against the same sales order line may have been approved in between. This delivery
        // is still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from its own
        // "already delivered" tally — same pattern as PurchaseOrder/GRPO's Approve-time re-check.
        if (order.SalesOrderId is not null)
        {
            var salesOrder = await dbContext.SalesOrders
                .Include(o => o.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(o => o.Id == order.SalesOrderId, cancellationToken);
            if (salesOrder is not null)
            {
                var referencedLineIds = order.Lines.Where(l => l.SalesOrderLineId.HasValue).Select(l => l.SalesOrderLineId!.Value).Distinct().ToList();
                var alreadyDelivered = await dbContext.DeliveryOrderLines
                    .Where(l => l.SalesOrderLineId.HasValue && referencedLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
                    .GroupBy(l => l.SalesOrderLineId!.Value)
                    .Select(g => new { SalesOrderLineId = g.Key, QtyShipped = g.Sum(l => l.QtyShipped) })
                    .ToDictionaryAsync(x => x.SalesOrderLineId, x => x.QtyShipped, cancellationToken);

                var lineInputs = order.Lines.Select(l => new DeliveryOrderLineInput(l.ItemId, l.QtyShipped, l.UomId, l.SalesOrderLineId)).ToList();
                var validationResult = CreateDeliveryOrderCommandHandler.ValidateAgainstSalesOrder(salesOrder, lineInputs, alreadyDelivered);
                if (!validationResult.IsSuccess)
                    return Result.Failure<DeliveryOrderResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "DELIVERY_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this delivery."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(decideResult.Error!);

        var postResult = await DeliveryPostingService.PostStockAndGlAsync(dbContext, issueStockLinesHandler, postGlJournalHandler, order, cancellationToken);
        if (!postResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(postResult.Error!);

        order.Status = "POSTED";
        await dbContext.DeliveryOrders.Where(d => d.Id == order.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "APPROVED", "ACTIVITY",
                "approved this delivery", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }
}
