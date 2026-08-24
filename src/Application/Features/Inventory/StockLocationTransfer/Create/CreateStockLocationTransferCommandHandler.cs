namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStockLocationTransferCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<CreateStockLocationTransferCommand, Result<StockLocationTransferResponse>>
{
    public async Task<Result<StockLocationTransferResponse>> HandleAsync(CreateStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
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

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "SLT"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<StockLocationTransferResponse>(numberResult.Error!);

        var transfer = new StockLocationTransfer
        {
            TransferNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            TransferDate = command.TransferDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new StockLocationTransferLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                FromLocationId = l.FromLocationId,
                ToLocationId = l.ToLocationId,
                Qty = l.Qty
            }).ToList()
        };

        dbContext.StockLocationTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in transfer.Lines)
        {
            line.Item = items[line.ItemId];
            line.FromLocation = locations[line.FromLocationId];
            line.ToLocation = locations[line.ToLocationId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_LOCATION_TRANSFER", transfer.Id.ToString(), transfer.BranchId, "CREATED", "ACTIVITY",
                "created this bin transfer", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockLocationTransferResponse>(notifyResult.Error!);

        return Result.Success(StockLocationTransferMapper.ToResponse(transfer));
    }
}
