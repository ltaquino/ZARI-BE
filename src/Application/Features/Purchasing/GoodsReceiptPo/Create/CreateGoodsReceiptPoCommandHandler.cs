namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGoodsReceiptPoCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(CreateGoodsReceiptPoCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to create goods receipts (PO) for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        PurchaseOrder? purchaseOrder = null;
        if (command.PurchaseOrderId.HasValue)
        {
            purchaseOrder = await dbContext.PurchaseOrders
                .Include(p => p.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(p => p.Id == command.PurchaseOrderId.Value, cancellationToken);
            if (purchaseOrder is null)
                return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.PurchaseOrderId}' was not found."));

            if (purchaseOrder.Status != "POSTED")
                return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.PurchaseOrderNotPosted", "The referenced purchase order must be approved before it can be received against."));
        }
        else if (command.Lines.Any(l => l.PurchaseOrderLineId.HasValue))
        {
            return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.UnexpectedPurchaseOrderLine", "Lines cannot reference a purchase order line unless this receipt itself references a purchase order."));
        }

        if (purchaseOrder is not null)
        {
            var referencedLineIds = command.Lines.Where(l => l.PurchaseOrderLineId.HasValue).Select(l => l.PurchaseOrderLineId!.Value).Distinct().ToList();
            var alreadyReceived = await dbContext.GoodsReceiptPoLines
                .Where(l => l.PurchaseOrderLineId.HasValue && referencedLineIds.Contains(l.PurchaseOrderLineId.Value) && l.GoodsReceiptPo.Status == "POSTED")
                .GroupBy(l => l.PurchaseOrderLineId!.Value)
                .Select(g => new { PurchaseOrderLineId = g.Key, QtyReceived = g.Sum(l => l.QtyReceived) })
                .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.QtyReceived, cancellationToken);

            var validationResult = ValidateAgainstPurchaseOrder(purchaseOrder, command.Lines, alreadyReceived);
            if (!validationResult.IsSuccess)
                return Result.Failure<GoodsReceiptPoResponse>(validationResult.Error!);
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Item.NotFound", "One or more items on this receipt were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this receipt were not found."));

        var locationIds = command.Lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).Distinct().ToList();
        if (locationIds.Count > 0)
        {
            var existingLocationCount = await dbContext.StorageLocations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken);
            if (existingLocationCount != locationIds.Count)
                return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("StorageLocation.NotFound", "One or more storage locations on this receipt were not found."));
        }

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "GRPO"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(numberResult.Error!);

        var receipt = new GoodsReceiptPo
        {
            GrpoNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            SupplierId = command.SupplierId,
            PurchaseOrderId = command.PurchaseOrderId,
            SupplierInvoiceNo = command.SupplierInvoiceNo,
            ReceiptDate = command.ReceiptDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new GoodsReceiptPoLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                QtyReceived = l.QtyReceived,
                UomId = l.UomId,
                UnitCost = l.UnitCost,
                LocationId = l.LocationId,
                PurchaseOrderLineId = l.PurchaseOrderLineId
            }).ToList()
        };

        dbContext.GoodsReceiptPos.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);

        receipt.Supplier = supplier;
        foreach (var line in receipt.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, "CREATED", "ACTIVITY",
                "created this goods receipt", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given order, for the same item)
    /// plus the actual cap: a purchase order line can't be received past its own Qty once
    /// <paramref name="alreadyReceivedByPurchaseOrderLine"/> (every OTHER posted goods receipt's
    /// claim on that line) is added to what this command itself is receiving.
    /// </summary>
    internal static Result ValidateAgainstPurchaseOrder(
        PurchaseOrder purchaseOrder, List<GoodsReceiptPoLineInput> lines, Dictionary<Guid, decimal> alreadyReceivedByPurchaseOrderLine)
    {
        if (lines.Any(l => !l.PurchaseOrderLineId.HasValue))
            return Result.Failure(Error.Validation("GoodsReceiptPo.LineMissingPurchaseOrderLine", "Every line must reference a specific purchase order line when this receipt is created against a purchase order."));

        var poLinesById = purchaseOrder.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!poLinesById.TryGetValue(line.PurchaseOrderLineId!.Value, out var poLine))
                return Result.Failure(Error.Validation("GoodsReceiptPo.InvalidPurchaseOrderLine", "One or more lines reference a purchase order line that doesn't belong to the referenced purchase order."));
            if (poLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("GoodsReceiptPo.ItemMismatch", $"A line's item must match the purchase order line it references ('{poLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.PurchaseOrderLineId!.Value))
        {
            var poLine = poLinesById[group.Key];
            var remaining = poLine.Qty - alreadyReceivedByPurchaseOrderLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.QtyReceived);
            if (requested > remaining)
                return Result.Failure(Error.Validation("GoodsReceiptPo.ExceedsOrderedQty", $"This receipt receives {requested} of '{poLine.Item.Code}' but only {remaining} of purchase order line quantity remains unreceived."));
        }

        return Result.Success();
    }
}
