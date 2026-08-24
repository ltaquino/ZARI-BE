namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateStockLocationTransferCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<UpdateStockLocationTransferCommand, Result<StockLocationTransferResponse>>
{
    public async Task<Result<StockLocationTransferResponse>> HandleAsync(UpdateStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (transfer is null)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{command.Id}' was not found."));

        if (transfer.Status != "DRAFT")
            return Result.Failure<StockLocationTransferResponse>(Error.Validation("StockLocationTransfer.NotDraft", "Only a draft bin transfer can be edited."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("Item.NotFound", "One or more items on this bin transfer were not found."));

        var locationIds = command.Lines.SelectMany(l => new[] { l.FromLocationId, l.ToLocationId }).Distinct().ToList();
        var locations = await dbContext.StorageLocations.Where(l => locationIds.Contains(l.Id) && l.WarehouseId == command.WarehouseId).ToDictionaryAsync(l => l.Id, cancellationToken);
        if (locations.Count != locationIds.Count)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("StorageLocation.NotFound", "One or more bins on this transfer were not found in the selected warehouse."));

        transfer.BranchId = command.BranchId;
        transfer.WarehouseId = command.WarehouseId;
        transfer.TransferDate = command.TransferDate;
        transfer.Remarks = command.Remarks;

        transfer.Lines.Clear();
        foreach (var line in command.Lines)
        {
            transfer.Lines.Add(new StockLocationTransferLine
            {
                ItemId = line.ItemId,
                BatchNo = line.BatchNo,
                SerialNo = line.SerialNo,
                FromLocationId = line.FromLocationId,
                ToLocationId = line.ToLocationId,
                Qty = line.Qty
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in transfer.Lines)
        {
            line.Item = items[line.ItemId];
            line.FromLocation = locations[line.FromLocationId];
            line.ToLocation = locations[line.ToLocationId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_LOCATION_TRANSFER", transfer.Id.ToString(), transfer.BranchId, "UPDATED", "ACTIVITY",
                "updated this bin transfer", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockLocationTransferResponse>(notifyResult.Error!);

        return Result.Success(StockLocationTransferMapper.ToResponse(transfer));
    }
}
