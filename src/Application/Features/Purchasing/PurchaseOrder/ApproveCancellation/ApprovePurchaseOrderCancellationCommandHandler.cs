namespace ZARI.Application.Features.Purchasing.PurchaseOrders.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the cancellation. No stock/GL
/// reversal is needed — approving a purchase order never posted anything in the first place.
/// </summary>
public sealed class ApprovePurchaseOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApprovePurchaseOrderCancellationCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(ApprovePurchaseOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("PURCHASE_ORDERS", cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (order.Status != "PENDING_CANCELLATION")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NotPendingCancellation", "Only a purchase order pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "PURCHASE_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this purchase order."));

        // Authoritative re-check, before deciding: a GRPO could have been approved against this PO
        // in the gap between the cancellation being requested and now being decided.
        var downstreamCheckResult = await RequestPurchaseOrderCancellationCommandHandler.CheckNoPostedReceiptsAsync(
            dbContext, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(downstreamCheckResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(decideResult.Error!);

        order.Status = "CANCELLED";
        order.CancelledBy = command.ApproverUserId;
        order.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
