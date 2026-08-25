namespace ZARI.Application.Features.Inventory.StockAdjustments.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateStockAdjustmentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateStockAdjustmentCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(UpdateStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Edit, adjustment.BranchId, cancellationToken))
            return Result.Failure<StockAdjustmentResponse>(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to update stock adjustments for this branch."));

        if (adjustment.Status != "DRAFT")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.NotDraft", "Only draft stock adjustments can be edited."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Include(i => i.BaseUom).Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("Item.NotFound", "One or more items on this adjustment were not found."));

        adjustment.BranchId = command.BranchId;
        adjustment.WarehouseId = command.WarehouseId;
        adjustment.AdjustmentDate = command.AdjustmentDate;
        adjustment.ReasonCode = command.ReasonCode;
        adjustment.Remarks = command.Remarks;

        adjustment.Lines.Clear();
        foreach (var line in command.Lines)
        {
            adjustment.Lines.Add(new StockAdjustmentLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                QtyBefore = line.QtyBefore,
                QtyAfter = line.QtyAfter,
                VarianceQty = line.QtyAfter - line.QtyBefore,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in adjustment.Lines)
        {
            line.Item = items[line.ItemId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "UPDATED", "ACTIVITY",
                "updated this stock adjustment", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
