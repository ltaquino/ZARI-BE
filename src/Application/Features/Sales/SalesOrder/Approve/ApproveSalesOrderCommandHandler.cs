namespace ZARI.Application.Features.Sales.SalesOrders.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Like PurchaseOrder, approving a sales order has no stock or GL side
/// effects — it's a commitment, not a financial transaction — so this is just the ApprovalRequest
/// decide plus a status flip.
/// </summary>
public sealed class ApproveSalesOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesOrderCommand, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(ApproveSalesOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Approve, order.BranchId, cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to approve sales orders for this branch."));

        if (order.Status != "PENDING_APPROVAL")
            return Result.Failure<SalesOrderResponse>(Error.Validation("SalesOrder.NotPendingApproval", "Only sales orders pending approval can be approved."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_ORDER" && r.EntityId == order.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this sales order."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(decideResult.Error!);

        order.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, "APPROVED", "ACTIVITY",
                "approved this sales order", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(notifyResult.Error!);

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }
}
