namespace ZARI.Application.Features.Sales.SalesReturns.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Mirrors CreateDeliveryOrderCommandHandler's shape (optional upstream document, structural +
/// remaining-qty validation against it, quick-post-with-real-side-effects): when
/// Company.SalesReturnQuickPostEnabled is on, this runs the exact same stock-receive + combined GL
/// reversal SalesReturnPostingService performs at Approve, right here at Create time, then marks the
/// document POSTED. Pure DRAFT-skip — no discount/threshold concept for a return, unlike Sales
/// Invoice/Sales Order's quick-post. If posting fails (e.g. a missing default GL account), the DRAFT
/// record already saved is left in place rather than rolled back, same as Delivery's own Create.
/// </summary>
public sealed class CreateSalesReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(CreateSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to create sales returns for this branch."));

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
            var referencedLineIds = command.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
            var alreadyReturned = await dbContext.SalesReturnLines
                .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesReturn.Status == "POSTED")
                .GroupBy(l => l.DeliveryOrderLineId!.Value)
                .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.QtyReturned) })
                .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

            var validationResult = ValidateAgainstDeliveryOrder(deliveryOrder, command.Lines, alreadyReturned);
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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "SRTN"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(numberResult.Error!);

        var salesReturn = new SalesReturn
        {
            ReturnNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            CustomerId = command.CustomerId,
            DeliveryOrderId = command.DeliveryOrderId,
            ReturnDate = command.ReturnDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new SalesReturnLine
            {
                ItemId = l.ItemId,
                QtyReturned = l.QtyReturned,
                UomId = l.UomId,
                UnitPrice = l.UnitPrice,
                DeliveryOrderLineId = l.DeliveryOrderLineId,
                SerialNo = l.SerialNo
            }).ToList()
        };

        dbContext.SalesReturns.Add(salesReturn);
        await dbContext.SaveChangesAsync(cancellationToken);

        salesReturn.Customer = customer;
        for (var i = 0; i < salesReturn.Lines.Count; i++)
        {
            var line = salesReturn.Lines[i];
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
            if (line.DeliveryOrderLineId.HasValue)
                line.DeliveryOrderLine = deliveryOrder!.Lines.First(dl => dl.Id == line.DeliveryOrderLineId.Value);
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        var quickPost = company?.SalesReturnQuickPostEnabled == true;

        if (quickPost)
        {
            // Only carries a value for lines with no DeliveryOrderLineId — see SalesReturnLineInput's
            // own doc comment for why this can't be persisted and re-read at a later Approve instead.
            var manualVatTypeByLineId = new Dictionary<Guid, string>();
            for (var i = 0; i < command.Lines.Count; i++)
            {
                if (!command.Lines[i].DeliveryOrderLineId.HasValue && command.Lines[i].VatType is { } vatType)
                    manualVatTypeByLineId[salesReturn.Lines[i].Id] = vatType;
            }

            var postResult = await SalesReturnPostingService.PostAsync(
                dbContext, receiveStockHandler, reverseIssueSerialHandler, postGlJournalHandler, salesReturn, manualVatTypeByLineId, cancellationToken);
            if (!postResult.IsSuccess)
                return Result.Failure<SalesReturnResponse>(postResult.Error!);

            // ReceiveStockCommand runs its own retryable transaction and calls ChangeTracker.Clear()
            // at the start of every attempt — that detaches the `salesReturn` this handler just
            // built, so mutating it and calling SaveChangesAsync would silently persist nothing.
            // ExecuteUpdateAsync writes directly, independent of whatever the tracker currently holds.
            salesReturn.Status = "POSTED";
            await dbContext.SalesReturns.Where(r => r.Id == salesReturn.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "POSTED"), cancellationToken);
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "CREATED", "ACTIVITY",
                quickPost ? "created this sales return (posted directly — quick-post enabled)" : "created this sales return",
                command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given delivery, for the same
    /// item) plus the actual cap: a delivery line can't be returned past its own QtyShipped once
    /// <paramref name="alreadyReturnedByDeliveryOrderLine"/> (every OTHER posted return's claim on
    /// that line) is added to what this command itself is returning.
    /// </summary>
    internal static Result ValidateAgainstDeliveryOrder(
        DeliveryOrder deliveryOrder, List<SalesReturnLineInput> lines, Dictionary<Guid, decimal> alreadyReturnedByDeliveryOrderLine)
    {
        if (lines.Any(l => !l.DeliveryOrderLineId.HasValue))
            return Result.Failure(Error.Validation("SalesReturn.LineMissingDeliveryOrderLine", "Every line must reference a specific delivery line when this return is created against a delivery."));

        var doLinesById = deliveryOrder.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!doLinesById.TryGetValue(line.DeliveryOrderLineId!.Value, out var doLine))
                return Result.Failure(Error.Validation("SalesReturn.InvalidDeliveryOrderLine", "One or more lines reference a delivery line that doesn't belong to the referenced delivery."));
            if (doLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("SalesReturn.ItemMismatch", $"A line's item must match the delivery line it references ('{doLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.DeliveryOrderLineId!.Value))
        {
            var doLine = doLinesById[group.Key];
            var remaining = doLine.QtyShipped - alreadyReturnedByDeliveryOrderLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.QtyReturned);
            if (requested > remaining)
                return Result.Failure(Error.Validation("SalesReturn.ExceedsDeliveredQty", $"This return requests {requested} of '{doLine.Item.Code}' but only {remaining} of delivered quantity remains unreturned."));
        }

        return Result.Success();
    }
}
