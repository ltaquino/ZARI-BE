namespace ZARI.Application.Features.Inventory.GoodsIssues.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateGoodsIssueCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<UpdateGoodsIssueCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(UpdateGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (issue.Status != "DRAFT")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotDraft", "Only draft goods issues can be edited."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        if (command.DestWarehouseId.HasValue)
        {
            var destWarehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.DestWarehouseId.Value, cancellationToken);
            if (!destWarehouseExists)
                return Result.Failure<GoodsIssueResponse>(Error.NotFound("Warehouse.NotFound", $"Destination warehouse with ID '{command.DestWarehouseId}' was not found."));
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Item.NotFound", "One or more items on this issue were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this issue were not found."));

        var isTransfer = command.ReferenceType == "STOCK_TRANSFER";
        issue.BranchId = command.BranchId;
        issue.WarehouseId = command.WarehouseId;
        issue.ReferenceType = command.ReferenceType;
        issue.DestBranchId = isTransfer ? command.DestBranchId : null;
        issue.DestWarehouseId = isTransfer ? command.DestWarehouseId : null;
        issue.ReasonCode = isTransfer ? null : command.ReasonCode;
        issue.GiDate = command.GiDate;
        issue.Remarks = command.Remarks;
        issue.StockTransferRequestRefNo = command.StockTransferRequestRefNo;
        issue.StockTransferRequestId = command.StockTransferRequestId;

        issue.Lines.Clear();
        foreach (var line in command.Lines)
        {
            issue.Lines.Add(new GoodsIssueLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                QtyIssued = line.QtyIssued,
                UomId = line.UomId,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in issue.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "UPDATED", "ACTIVITY",
                "updated this goods issue", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
