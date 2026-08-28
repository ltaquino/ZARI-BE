namespace ZARI.Application.Features.Inventory.StockAdjustments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStockAdjustmentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateStockAdjustmentCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(CreateStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<StockAdjustmentResponse>(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to create stock adjustments for this branch."));

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

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "ADJ"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(numberResult.Error!);

        var adjustment = new StockAdjustment
        {
            AdjustmentNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            AdjustmentDate = command.AdjustmentDate,
            ReasonCode = command.ReasonCode,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new StockAdjustmentLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                QtyBefore = l.QtyBefore,
                QtyAfter = l.QtyAfter,
                VarianceQty = l.QtyAfter - l.QtyBefore,
                UnitCost = l.UnitCost
            }).ToList()
        };

        dbContext.StockAdjustments.Add(adjustment);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in adjustment.Lines)
        {
            line.Item = items[line.ItemId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "CREATED", "ACTIVITY",
                "created this stock adjustment", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
