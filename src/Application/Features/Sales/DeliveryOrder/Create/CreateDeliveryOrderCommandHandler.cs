namespace ZARI.Application.Features.Sales.DeliveryOrders.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Mirrors CreateGoodsReceiptPoCommandHandler's shape (optional upstream order, structural +
/// remaining-qty validation against it) but adds the module's first quick-post-with-real-side-
/// effects case: when Company.DeliveryQuickPostEnabled is on, this doesn't just flip Status — it
/// runs the exact same stock-issue + GL-posting DeliveryPostingService performs at Approve, right
/// here at Create time, then marks the document POSTED. If that posting fails (e.g. insufficient
/// stock), the DRAFT record this method already saved is left in place rather than rolled back —
/// the encoder can fix the issue and route it through Submit/Approve normally instead of losing
/// the work.
/// </summary>
public sealed class CreateDeliveryOrderCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(CreateDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to create deliveries for this branch."));

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
            var alreadyDelivered = await dbContext.DeliveryOrderLines
                .Where(l => l.SalesOrderLineId.HasValue && referencedLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
                .GroupBy(l => l.SalesOrderLineId!.Value)
                .Select(g => new { SalesOrderLineId = g.Key, QtyShipped = g.Sum(l => l.QtyShipped) })
                .ToDictionaryAsync(x => x.SalesOrderLineId, x => x.QtyShipped, cancellationToken);

            var validationResult = ValidateAgainstSalesOrder(salesOrder, command.Lines, alreadyDelivered);
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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "DO"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(numberResult.Error!);

        var order = new DeliveryOrder
        {
            DoNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            CustomerId = command.CustomerId,
            SalesOrderId = command.SalesOrderId,
            DeliveryDate = command.DeliveryDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new DeliveryOrderLine
            {
                ItemId = l.ItemId,
                QtyShipped = l.QtyShipped,
                UomId = l.UomId,
                SalesOrderLineId = l.SalesOrderLineId
            }).ToList()
        };

        dbContext.DeliveryOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        order.Customer = customer;
        foreach (var line in order.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        var quickPost = company?.DeliveryQuickPostEnabled == true;

        if (quickPost)
        {
            var postResult = await DeliveryPostingService.PostStockAndGlAsync(dbContext, issueStockLinesHandler, postGlJournalHandler, order, cancellationToken);
            if (!postResult.IsSuccess)
                return Result.Failure<DeliveryOrderResponse>(postResult.Error!);

            order.Status = "POSTED";
            await dbContext.DeliveryOrders.Where(d => d.Id == order.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, "POSTED"), cancellationToken);
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "CREATED", "ACTIVITY",
                quickPost ? "created this delivery (posted directly — quick-post enabled)" : "created this delivery",
                command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given order, for the same item)
    /// plus the actual cap: a sales order line can't be shipped past its own Qty once
    /// <paramref name="alreadyDeliveredBySalesOrderLine"/> (every OTHER posted delivery's claim on
    /// that line) is added to what this command itself is shipping.
    /// </summary>
    internal static Result ValidateAgainstSalesOrder(
        SalesOrder salesOrder, List<DeliveryOrderLineInput> lines, Dictionary<Guid, decimal> alreadyDeliveredBySalesOrderLine)
    {
        if (lines.Any(l => !l.SalesOrderLineId.HasValue))
            return Result.Failure(Error.Validation("DeliveryOrder.LineMissingSalesOrderLine", "Every line must reference a specific sales order line when this delivery is created against a sales order."));

        var soLinesById = salesOrder.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!soLinesById.TryGetValue(line.SalesOrderLineId!.Value, out var soLine))
                return Result.Failure(Error.Validation("DeliveryOrder.InvalidSalesOrderLine", "One or more lines reference a sales order line that doesn't belong to the referenced sales order."));
            if (soLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("DeliveryOrder.ItemMismatch", $"A line's item must match the sales order line it references ('{soLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.SalesOrderLineId!.Value))
        {
            var soLine = soLinesById[group.Key];
            var remaining = soLine.Qty - alreadyDeliveredBySalesOrderLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.QtyShipped);
            if (requested > remaining)
                return Result.Failure(Error.Validation("DeliveryOrder.ExceedsOrderedQty", $"This delivery ships {requested} of '{soLine.Item.Code}' but only {remaining} of sales order line quantity remains undelivered."));
        }

        return Result.Success();
    }
}
