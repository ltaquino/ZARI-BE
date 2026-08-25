namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdatePurchaseOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdatePurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    public async Task<Result<PurchaseOrderResponse>> HandleAsync(UpdatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Edit, order.BranchId, cancellationToken))
            return Result.Failure<PurchaseOrderResponse>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to update purchase orders for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure<PurchaseOrderResponse>(Error.Validation("PurchaseOrder.NotDraft", "Only draft purchase orders can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Item.NotFound", "One or more items on this purchase order were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<PurchaseOrderResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this purchase order were not found."));

        order.BranchId = command.BranchId;
        order.SupplierId = command.SupplierId;
        order.OrderDate = command.OrderDate;
        order.ExpectedDate = command.ExpectedDate;
        order.Remarks = command.Remarks;

        order.Lines.Clear();
        foreach (var line in command.Lines)
        {
            order.Lines.Add(new PurchaseOrderLine
            {
                ItemId = line.ItemId,
                Qty = line.Qty,
                UomId = line.UomId,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        order.Supplier = supplier;
        foreach (var line in order.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("PURCHASE_ORDER", order.Id.ToString(), order.BranchId, "UPDATED", "ACTIVITY",
                "updated this purchase order", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<PurchaseOrderResponse>(notifyResult.Error!);

        return Result.Success(PurchaseOrderMapper.ToResponse(order));
    }
}
