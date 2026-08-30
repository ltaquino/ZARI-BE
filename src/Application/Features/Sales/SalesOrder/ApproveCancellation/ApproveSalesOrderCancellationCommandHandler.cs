namespace ZARI.Application.Features.Sales.SalesOrders.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the cancellation. No stock/GL
/// reversal is needed — approving a sales order never posted anything in the first place.
/// </summary>
public sealed class ApproveSalesOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesOrderCancellationCommand, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(ApproveSalesOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("SALES_ORDERS", cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (order.Status != "PENDING_CANCELLATION")
            return Result.Failure<SalesOrderResponse>(Error.Validation("SalesOrder.NotPendingCancellation", "Only a sales order pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this sales order."));

        // Authoritative re-check, before deciding: a Delivery could have been approved against this
        // order in the gap between the cancellation being requested and now being decided.
        var downstreamCheckResult = await RequestSalesOrderCancellationCommandHandler.CheckNoPostedDeliveriesAsync(
            dbContext, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(downstreamCheckResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(decideResult.Error!);

        order.Status = "CANCELLED";
        order.CancelledBy = command.ApproverUserId;
        order.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(notifyResult.Error!);

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }
}
