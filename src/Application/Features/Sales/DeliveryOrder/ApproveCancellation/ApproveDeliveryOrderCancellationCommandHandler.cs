namespace ZARI.Application.Features.Sales.DeliveryOrders.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.RequestCancellation;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors ApproveGoodsReceiptPoCancellationCommandHandler: reverse the stock ledger
/// movements, reverse the posted GL journal, then decide the cancellation request.
/// </summary>
public sealed class ApproveDeliveryOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveDeliveryOrderCancellationCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(ApproveDeliveryOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("DELIVERIES", cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (order.Status != "PENDING_CANCELLATION")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.NotPendingCancellation", "Only a delivery pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "DELIVERY_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this delivery."));

        // Authoritative re-check, before any reversal side-effect: a Sales Invoice or Sales Return
        // could have been approved against this delivery in the gap between the cancellation being
        // requested and now being decided.
        var downstreamCheckResult = await RequestDeliveryOrderCancellationCommandHandler.CheckNoDownstreamPostedDocumentsAsync(
            dbContext, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(downstreamCheckResult.Error!);

        var lineIds = order.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("DeliveryOrderLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(reverseStockResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("DeliveryOrder", order.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {order.DoNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(decideResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `order` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        order.Status = "CANCELLED";
        order.CancelledBy = command.ApproverUserId;
        order.CancelledAt = cancelledAt;
        await dbContext.DeliveryOrders.Where(d => d.Id == order.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, "CANCELLED")
                .SetProperty(d => d.CancelledBy, command.ApproverUserId)
                .SetProperty(d => d.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }
}
