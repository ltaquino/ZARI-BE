namespace ZARI.Application.Features.Sales.SalesOrders.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Application.Features.Sales.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Mirrors CreatePurchaseOrderCommandHandler almost exactly — a Sales Order has no upstream
/// document to validate against (it's the top of the Order-to-Cash chain) and no stock/GL effect,
/// so this is simpler. The one addition Purchasing's PO never needed: the quick-post mechanic —
/// when Company.SalesOrderQuickPostEnabled is on and no line/header discount breaches
/// Company.MaxUnapprovedDiscountPct, the order posts directly instead of starting at DRAFT.
/// </summary>
public sealed class CreateSalesOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateSalesOrderCommand, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(CreateSalesOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to create sales orders for this branch."));

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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "SO"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(numberResult.Error!);

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        var exceedsThreshold = DiscountThresholdPolicy.ExceedsThreshold(
            company?.MaxUnapprovedDiscountPct, command.DiscountPct, command.Lines.Select(l => l.DiscountPct));
        var quickPost = company is { SalesOrderQuickPostEnabled: true } && !exceedsThreshold;

        var order = new SalesOrder
        {
            SoNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            OrderDate = command.OrderDate,
            ExpectedDeliveryDate = command.ExpectedDeliveryDate,
            Status = quickPost ? "POSTED" : "DRAFT",
            Remarks = command.Remarks,
            DiscountPct = command.DiscountPct,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new SalesOrderLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UomId = l.UomId,
                UnitPrice = l.UnitPrice,
                DiscountPct = l.DiscountPct,
                DiscountSourceType = l.DiscountSourceType,
                DiscountSourceId = l.DiscountSourceId
            }).ToList()
        };

        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        order.Customer = customer;
        foreach (var line in order.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_ORDER", order.Id.ToString(), order.BranchId, "CREATED", "ACTIVITY",
                quickPost ? "created this sales order (posted directly — quick-post enabled)" : "created this sales order",
                command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesOrderResponse>(notifyResult.Error!);

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }
}
