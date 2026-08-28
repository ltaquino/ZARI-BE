namespace ZARI.Application.Features.Purchasing.GoodsReturns.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGoodsReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateGoodsReturnCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(CreateGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to create goods returns for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        GoodsReceiptPo? goodsReceiptPo = null;
        if (command.GoodsReceiptPoId.HasValue)
        {
            goodsReceiptPo = await dbContext.GoodsReceiptPos
                .Include(g => g.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(g => g.Id == command.GoodsReceiptPoId.Value, cancellationToken);
            if (goodsReceiptPo is null)
                return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.GoodsReceiptPoId}' was not found."));

            if (goodsReceiptPo.Status != "POSTED")
                return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.GoodsReceiptPoNotPosted", "The referenced goods receipt (PO) must be posted before items can be returned against it."));
        }
        else if (command.Lines.Any(l => l.GoodsReceiptPoLineId.HasValue))
        {
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.UnexpectedGoodsReceiptPoLine", "Lines cannot reference a goods receipt (PO) line unless this return itself references a goods receipt (PO)."));
        }

        if (goodsReceiptPo is not null)
        {
            var referencedLineIds = command.Lines.Where(l => l.GoodsReceiptPoLineId.HasValue).Select(l => l.GoodsReceiptPoLineId!.Value).Distinct().ToList();
            var alreadyReturned = await dbContext.GoodsReturnLines
                .Where(l => l.GoodsReceiptPoLineId.HasValue && referencedLineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.GoodsReturn.Status == "POSTED")
                .GroupBy(l => l.GoodsReceiptPoLineId!.Value)
                .Select(g => new { GoodsReceiptPoLineId = g.Key, Qty = g.Sum(l => l.QtyReturned) })
                .ToDictionaryAsync(x => x.GoodsReceiptPoLineId, x => x.Qty, cancellationToken);

            var validationResult = ValidateAgainstGoodsReceiptPo(goodsReceiptPo, command.Lines, alreadyReturned);
            if (!validationResult.IsSuccess)
                return Result.Failure<GoodsReturnResponse>(validationResult.Error!);
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

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "GRTN"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(numberResult.Error!);

        var goodsReturn = new GoodsReturn
        {
            ReturnNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            SupplierId = command.SupplierId,
            GoodsReceiptPoId = command.GoodsReceiptPoId,
            ReasonCode = command.ReasonCode,
            ReturnDate = command.ReturnDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new GoodsReturnLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                QtyReturned = l.QtyReturned,
                UomId = l.UomId,
                UnitCost = l.UnitCost,
                GoodsReceiptPoLineId = l.GoodsReceiptPoLineId
            }).ToList()
        };

        dbContext.GoodsReturns.Add(goodsReturn);
        await dbContext.SaveChangesAsync(cancellationToken);

        goodsReturn.Supplier = supplier;
        foreach (var line in goodsReturn.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "CREATED", "ACTIVITY",
                "created this goods return", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given receipt, for the same item)
    /// plus the actual cap: a goods receipt line can't be returned past its own QtyReceived once
    /// <paramref name="alreadyReturnedByGoodsReceiptPoLine"/> (every OTHER posted goods return's
    /// claim on that line) is added to what this command itself is returning.
    /// </summary>
    internal static Result ValidateAgainstGoodsReceiptPo(
        GoodsReceiptPo goodsReceiptPo, List<GoodsReturnLineInput> lines, Dictionary<Guid, decimal> alreadyReturnedByGoodsReceiptPoLine)
    {
        if (lines.Any(l => !l.GoodsReceiptPoLineId.HasValue))
            return Result.Failure(Error.Validation("GoodsReturn.LineMissingGoodsReceiptPoLine", "Every line must reference a specific goods receipt (PO) line when this return is created against a goods receipt (PO)."));

        var grpoLinesById = goodsReceiptPo.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!grpoLinesById.TryGetValue(line.GoodsReceiptPoLineId!.Value, out var grpoLine))
                return Result.Failure(Error.Validation("GoodsReturn.InvalidGoodsReceiptPoLine", "One or more lines reference a goods receipt (PO) line that doesn't belong to the referenced goods receipt (PO)."));
            if (grpoLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("GoodsReturn.ItemMismatch", $"A line's item must match the goods receipt (PO) line it references ('{grpoLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.GoodsReceiptPoLineId!.Value))
        {
            var grpoLine = grpoLinesById[group.Key];
            var remaining = grpoLine.QtyReceived - alreadyReturnedByGoodsReceiptPoLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.QtyReturned);
            if (requested > remaining)
                return Result.Failure(Error.Validation("GoodsReturn.ExceedsReceivedQty", $"This return requests {requested} of '{grpoLine.Item.Code}' but only {remaining} of goods receipt (PO) line quantity remains unreturned."));
        }

        return Result.Success();
    }
}
