namespace ZARI.Application.Features.Sales.SalesOrders.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateSalesOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesOrderCommand, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(UpdateSalesOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Edit, order.BranchId, cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to update sales orders for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure<SalesOrderResponse>(Error.Validation("SalesOrder.NotDraft", "Only draft sales orders can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("Item.NotFound", "One or more items on this sales order were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this sales order were not found."));

        order.BranchId = command.BranchId;
        order.CustomerId = command.CustomerId;
        order.OrderDate = command.OrderDate;
        order.ExpectedDeliveryDate = command.ExpectedDeliveryDate;
        order.Remarks = command.Remarks;
        order.DiscountPct = command.DiscountPct;

        order.Lines.Clear();
        foreach (var line in command.Lines)
        {
            order.Lines.Add(new SalesOrderLine
            {
                ItemId = line.ItemId,
                Qty = line.Qty,
                UomId = line.UomId,
                UnitPrice = line.UnitPrice,
                DiscountPct = line.DiscountPct,
                DiscountSourceType = line.DiscountSourceType,
                DiscountSourceId = line.DiscountSourceId
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
            new CreateNotificationCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, "UPDATED", "ACTIVITY",
                "updated this sales order", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(notifyResult.Error!);

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }
}
