namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED purchase order has to go through RequestPurchaseOrderCancellation instead.
/// </summary>
public sealed class CancelPurchaseOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelPurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(CancelPurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Cancel, order.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to cancel purchase orders for this branch."));

        if (order.Status == "CANCELLED")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.AlreadyCancelled", "This purchase order is already cancelled."));

        if (order.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.RequiresCancellationRequest", "A posted purchase order must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("PURCHASE_ORDER", order.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(cancelPendingResult.Error!);

        order.Status = "CANCELLED";
        order.CancelledBy = command.CancelledBy;
        order.CancelledAt = DateTimeOffset.UtcNow;
        order.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this purchase order — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
