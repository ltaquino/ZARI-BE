namespace ZARI.Application.Features.Sales.DeliveryOrders.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no stock/GL reversal
/// is needed. A POSTED delivery has to go through RequestDeliveryOrderCancellation instead.
/// </summary>
public sealed class CancelDeliveryOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(CancelDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Cancel, order.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to cancel deliveries for this branch."));

        if (order.Status == "CANCELLED")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.AlreadyCancelled", "This delivery is already cancelled."));

        if (order.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.RequiresCancellationRequest", "A posted delivery must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("DELIVERY_ORDER", order.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(cancelPendingResult.Error!);

        order.Status = "CANCELLED";
        order.CancelledBy = command.CancelledBy;
        order.CancelledAt = DateTimeOffset.UtcNow;
        order.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this delivery — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }
}
