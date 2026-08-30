namespace ZARI.Application.Features.Sales.SalesReturns.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateSalesReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesReturnCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(UpdateSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Edit, salesReturn.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to update sales returns for this branch."));

        if (salesReturn.Status != "DRAFT")
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NotDraft", "Only draft sales returns can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        DeliveryOrder? deliveryOrder = null;
        if (command.DeliveryOrderId.HasValue)
        {
            deliveryOrder = await dbContext.DeliveryOrders
                .Include(d => d.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(d => d.Id == command.DeliveryOrderId.Value, cancellationToken);
            if (deliveryOrder is null)
                return Result.Failure<SalesReturnResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.DeliveryOrderId}' was not found."));

            if (deliveryOrder.Status != "POSTED")
                return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.DeliveryOrderNotPosted", "The referenced delivery must be posted before items can be returned against it."));
        }
        else if (command.Lines.Any(l => l.DeliveryOrderLineId.HasValue))
        {
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.UnexpectedDeliveryOrderLine", "Lines cannot reference a delivery line unless this return itself references a delivery."));
        }

        if (deliveryOrder is not null)
        {
            // This return is DRAFT (checked above), so it's never itself among the "POSTED" sales
            // returns being summed here — no self-exclusion needed.
            var referencedLineIds = command.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
            var alreadyReturned = await dbContext.SalesReturnLines
                .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesReturn.Status == "POSTED")
                .GroupBy(l => l.DeliveryOrderLineId!.Value)
                .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.QtyReturned) })
                .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

            var validationResult = CreateSalesReturnCommandHandler.ValidateAgainstDeliveryOrder(deliveryOrder, command.Lines, alreadyReturned);
            if (!validationResult.IsSuccess)
                return Result.Failure<SalesReturnResponse>(validationResult.Error!);
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("Item.NotFound", "One or more items on this return were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this return were not found."));

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        salesReturn.BranchId = command.BranchId;
        salesReturn.WarehouseId = command.WarehouseId;
        salesReturn.CustomerId = command.CustomerId;
        salesReturn.DeliveryOrderId = command.DeliveryOrderId;
        salesReturn.ReturnDate = command.ReturnDate;
        salesReturn.Remarks = command.Remarks;
        salesReturn.CostCenterId = command.CostCenterId;

        salesReturn.Lines.Clear();
        foreach (var line in command.Lines)
        {
            salesReturn.Lines.Add(new SalesReturnLine
            {
                ItemId = line.ItemId,
                QtyReturned = line.QtyReturned,
                UomId = line.UomId,
                UnitPrice = line.UnitPrice,
                DeliveryOrderLineId = line.DeliveryOrderLineId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        salesReturn.Customer = customer;
        foreach (var line in salesReturn.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "UPDATED", "ACTIVITY",
                "updated this sales return", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
