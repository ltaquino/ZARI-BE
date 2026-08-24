namespace ZARI.Application.Features.Inventory.GoodsReceipts.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateGoodsReceiptCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<UpdateGoodsReceiptCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(UpdateGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (receipt.Status != "DRAFT")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotDraft", "Only draft goods receipts can be edited."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("Item.NotFound", "One or more items on this receipt were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this receipt were not found."));

        var locationIds = command.Lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).Distinct().ToList();
        if (locationIds.Count > 0)
        {
            var existingLocationCount = await dbContext.StorageLocations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken);
            if (existingLocationCount != locationIds.Count)
                return Result.Failure<GoodsReceiptResponse>(Error.NotFound("StorageLocation.NotFound", "One or more storage locations on this receipt were not found."));
        }

        receipt.BranchId = command.BranchId;
        receipt.WarehouseId = command.WarehouseId;
        receipt.ReceiptType = command.ReceiptType;
        receipt.ReceivedBy = command.ReceivedBy;
        receipt.GrDate = command.GrDate;
        receipt.Remarks = command.Remarks;
        receipt.GoodsIssueRefNo = command.GoodsIssueRefNo;
        receipt.GoodsIssueId = command.ReceiptType == "TRANSFER_IN" ? command.GoodsIssueId : null;
        receipt.ReasonCode = command.ReasonCode;

        receipt.Lines.Clear();
        foreach (var line in command.Lines)
        {
            receipt.Lines.Add(new GoodsReceiptLine
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

        foreach (var line in receipt.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "UPDATED", "ACTIVITY",
                "updated this goods receipt", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
