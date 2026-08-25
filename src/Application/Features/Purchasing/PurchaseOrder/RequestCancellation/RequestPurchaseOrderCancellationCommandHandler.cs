namespace ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch approver flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestPurchaseOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestPurchaseOrderCancellationCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(RequestPurchaseOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Cancel, order.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to request cancellation of purchase orders for this branch."));

        if (order.Status != "POSTED")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NotPosted", "Only a posted purchase order can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(submitResult.Error!);

        order.Status = "PENDING_CANCELLATION";
        order.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
