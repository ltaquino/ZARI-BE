namespace ZARI.Application.Features.Purchasing.GoodsReturns.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateGoodsReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateGoodsReturnCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(UpdateGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Edit, goodsReturn.BranchId, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to update goods returns for this branch."));

        if (goodsReturn.Status != "DRAFT")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NotDraft", "Only draft goods returns can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        if (command.GoodsReceiptPoId.HasValue)
        {
            var grpoExists = await dbContext.GoodsReceiptPos.AnyAsync(g => g.Id == command.GoodsReceiptPoId.Value, cancellationToken);
            if (!grpoExists)
                return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.GoodsReceiptPoId}' was not found."));
        }

        var reasonExists = await dbContext.PurchaseReturnReasons.AnyAsync(r => r.Code == command.ReasonCode, cancellationToken);
        if (!reasonExists)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("PurchaseReturnReason.NotFound", $"Purchase return reason '{command.ReasonCode}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Item.NotFound", "One or more items on this return were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this return were not found."));

        goodsReturn.BranchId = command.BranchId;
        goodsReturn.WarehouseId = command.WarehouseId;
        goodsReturn.SupplierId = command.SupplierId;
        goodsReturn.GoodsReceiptPoId = command.GoodsReceiptPoId;
        goodsReturn.ReasonCode = command.ReasonCode;
        goodsReturn.ReturnDate = command.ReturnDate;
        goodsReturn.Remarks = command.Remarks;

        goodsReturn.Lines.Clear();
        foreach (var line in command.Lines)
        {
            goodsReturn.Lines.Add(new GoodsReturnLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                QtyReturned = line.QtyReturned,
                UomId = line.UomId,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        goodsReturn.Supplier = supplier;
        foreach (var line in goodsReturn.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "UPDATED", "ACTIVITY",
                "updated this goods return", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }
}
