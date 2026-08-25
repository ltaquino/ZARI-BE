namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Submit;

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

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitPurchaseOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitPurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Edit, order.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to submit purchase orders for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NotDraft", "Only draft purchase orders can be submitted for approval."));

        if (order.Lines.Count == 0)
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(submitResult.Error!);

        order.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this purchase order for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
