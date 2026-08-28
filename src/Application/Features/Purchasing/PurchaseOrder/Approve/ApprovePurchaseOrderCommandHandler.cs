namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Create;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Unlike GoodsReceipt, approving a purchase order has no stock or GL
/// side effects — it's just a commitment, not a financial transaction — so this is only the
/// ApprovalRequest decide plus a status flip.
/// </summary>
public sealed class ApprovePurchaseOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApprovePurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(ApprovePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Approve, order.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to approve purchase orders for this branch."));

        if (order.Status != "PENDING_APPROVAL")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NotPendingApproval", "Only purchase orders pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
        // purchase order against the same purchase request line may have been approved in between.
        // This order is still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from
        // its own "already ordered" tally — same pattern as OutgoingPayment's Approve-time re-check.
        if (order.PurchaseRequestId is not null)
        {
            var purchaseRequest = await dbContext.PurchaseRequests
                .Include(r => r.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(r => r.Id == order.PurchaseRequestId, cancellationToken);
            if (purchaseRequest is not null)
            {
                var referencedLineIds = order.Lines.Where(l => l.PurchaseRequestLineId.HasValue).Select(l => l.PurchaseRequestLineId!.Value).Distinct().ToList();
                var alreadyOrdered = await dbContext.PurchaseOrderLines
                    .Where(l => l.PurchaseRequestLineId.HasValue && referencedLineIds.Contains(l.PurchaseRequestLineId.Value) && l.PurchaseOrder.Status == "POSTED")
                    .GroupBy(l => l.PurchaseRequestLineId!.Value)
                    .Select(g => new { PurchaseRequestLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                    .ToDictionaryAsync(x => x.PurchaseRequestLineId, x => x.Qty, cancellationToken);

                var lineInputs = order.Lines.Select(l => new PurchaseOrderLineInput(l.ItemId, l.Qty, l.UomId, l.UnitCost, l.PurchaseRequestLineId)).ToList();
                var validationResult = CreatePurchaseOrderCommandHandler.ValidateAgainstPurchaseRequest(purchaseRequest, lineInputs, alreadyOrdered);
                if (!validationResult.IsSuccess)
                    return Result.Failure<PurchaseOrderResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "PURCHASE_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this purchase order."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(decideResult.Error!);

        order.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "APPROVED", "ACTIVITY",
                "approved this purchase order", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
