namespace ZARI.Application.Features.Sales.DeliveryOrders.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateDeliveryOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(UpdateDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Edit, order.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to update deliveries for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.NotDraft", "Only draft deliveries can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        SalesOrder? salesOrder = null;
        if (command.SalesOrderId.HasValue)
        {
            salesOrder = await dbContext.SalesOrders
                .Include(o => o.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(o => o.Id == command.SalesOrderId.Value, cancellationToken);
            if (salesOrder is null)
                return Result.Failure<DeliveryOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.SalesOrderId}' was not found."));

            if (salesOrder.Status != "POSTED")
                return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.SalesOrderNotPosted", "The referenced sales order must be approved before it can be delivered against."));
        }
        else if (command.Lines.Any(l => l.SalesOrderLineId.HasValue))
        {
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.UnexpectedSalesOrderLine", "Lines cannot reference a sales order line unless this delivery itself references a sales order."));
        }

        if (salesOrder is not null)
        {
            var referencedLineIds = command.Lines.Where(l => l.SalesOrderLineId.HasValue).Select(l => l.SalesOrderLineId!.Value).Distinct().ToList();
            // Excludes this delivery's own (pre-update) lines from the "already delivered" tally —
            // this delivery is still DRAFT (not POSTED) so it's naturally excluded already.
            var alreadyDelivered = await dbContext.DeliveryOrderLines
                .Where(l => l.SalesOrderLineId.HasValue && referencedLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
                .GroupBy(l => l.SalesOrderLineId!.Value)
                .Select(g => new { SalesOrderLineId = g.Key, QtyShipped = g.Sum(l => l.QtyShipped) })
                .ToDictionaryAsync(x => x.SalesOrderLineId, x => x.QtyShipped, cancellationToken);

            var validationResult = CreateDeliveryOrderCommandHandler.ValidateAgainstSalesOrder(salesOrder, command.Lines, alreadyDelivered);
            if (!validationResult.IsSuccess)
                return Result.Failure<DeliveryOrderResponse>(validationResult.Error!);
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Item.NotFound", "One or more items on this delivery were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this delivery were not found."));

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        order.BranchId = command.BranchId;
        order.WarehouseId = command.WarehouseId;
        order.CustomerId = command.CustomerId;
        order.SalesOrderId = command.SalesOrderId;
        order.DeliveryDate = command.DeliveryDate;
        order.Remarks = command.Remarks;
        order.CostCenterId = command.CostCenterId;

        order.Lines.Clear();
        foreach (var line in command.Lines)
        {
            order.Lines.Add(new DeliveryOrderLine
            {
                ItemId = line.ItemId,
                QtyShipped = line.QtyShipped,
                UomId = line.UomId,
                SalesOrderLineId = line.SalesOrderLineId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        order.Customer = customer;
        foreach (var line in order.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "UPDATED", "ACTIVITY",
                "updated this delivery", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }
}
