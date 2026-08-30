namespace ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch approver flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestSalesOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestSalesOrderCancellationCommand, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(RequestSalesOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Cancel, order.BranchId, cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to request cancellation of sales orders for this branch."));

        if (order.Status != "POSTED")
            return Result.Failure<SalesOrderResponse>(Error.Validation("SalesOrder.NotPosted", "Only a posted sales order can have its cancellation requested."));

        var downstreamCheckResult = await CheckNoPostedDeliveriesAsync(dbContext, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(downstreamCheckResult.Error!);

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(submitResult.Error!);

        order.Status = "PENDING_CANCELLATION";
        order.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(notifyResult.Error!);

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }

    /// <summary>
    /// Blocks cancelling a Sales Order that's already been (partially or fully) delivered — mirrors
    /// RequestPurchaseOrderCancellationCommandHandler.CheckNoPostedReceiptsAsync exactly, one level
    /// down the chain. DeliveryOrder doesn't exist yet as a buildable module (Wave 2), but its table
    /// and SalesOrderLineId FK already do (Wave 0), so this check is live and protective from day
    /// one — it just can never find a match until Delivery actually ships. Checked here (friendly,
    /// at request time) and again in ApproveSalesOrderCancellationCommandHandler (authoritative,
    /// since a Delivery could be approved in the gap between request and approval).
    /// </summary>
    internal static async Task<Result> CheckNoPostedDeliveriesAsync(IAppDbContext dbContext, List<Guid> lineIds, CancellationToken cancellationToken)
    {
        var hasPostedDelivery = await dbContext.DeliveryOrderLines
            .AnyAsync(l => l.SalesOrderLineId.HasValue && lineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED", cancellationToken);
        return hasPostedDelivery
            ? Result.Failure(Error.Validation("SalesOrder.HasPostedDelivery", "This sales order can't be cancelled — a posted delivery already references it."))
            : Result.Success();
    }
}
