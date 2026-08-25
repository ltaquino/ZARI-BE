namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateGoodsReceiptPoCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateGoodsReceiptPoCommand, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(UpdateGoodsReceiptPoCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Edit, receipt.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to update goods receipts (PO) for this branch."));

        if (receipt.Status != "DRAFT")
            return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.NotDraft", "Only draft goods receipts can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        if (command.PurchaseOrderId.HasValue)
        {
            var poExists = await dbContext.PurchaseOrders.AnyAsync(p => p.Id == command.PurchaseOrderId.Value, cancellationToken);
            if (!poExists)
                return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.PurchaseOrderId}' was not found."));
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

        receipt.BranchId = command.BranchId;
        receipt.WarehouseId = command.WarehouseId;
        receipt.SupplierId = command.SupplierId;
        receipt.PurchaseOrderId = command.PurchaseOrderId;
        receipt.SupplierInvoiceNo = command.SupplierInvoiceNo;
        receipt.ReceiptDate = command.ReceiptDate;
        receipt.Remarks = command.Remarks;

        receipt.Lines.Clear();
        foreach (var line in command.Lines)
        {
            receipt.Lines.Add(new GoodsReceiptPoLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                QtyReceived = line.QtyReceived,
                UomId = line.UomId,
                UnitCost = line.UnitCost,
                LocationId = line.LocationId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        receipt.Supplier = supplier;
        foreach (var line in receipt.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, "UPDATED", "ACTIVITY",
                "updated this goods receipt", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }
}
