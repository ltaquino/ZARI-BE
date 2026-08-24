namespace ZARI.Application.Features.Inventory.StockLocationBalances.Receive;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationBalances.Shared;
using ZARI.Domain.Common;

public sealed class ReceiveIntoLocationCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReceiveIntoLocationCommand, Result>
{
    public async Task<Result> HandleAsync(ReceiveIntoLocationCommand command, CancellationToken cancellationToken = default)
    {
        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var locationExists = await dbContext.StorageLocations.AnyAsync(l => l.Id == command.LocationId, cancellationToken);
        if (!locationExists)
            return Result.Failure(Error.NotFound("StorageLocation.NotFound", $"Storage location with ID '{command.LocationId}' was not found."));

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Same retry-safety and locking rationale as ReceiveStockCommandHandler — see
            // StockLocationBalanceLocker for the FOR UPDATE gap-locking this relies on.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var lockedRows = await StockLocationBalanceLocker.LockItemWarehouseAsync(dbContext, command.ItemId, command.WarehouseId, cancellationToken);
            var balance = StockLocationBalanceLocker.GetOrCreate(dbContext, lockedRows, command.ItemId, command.WarehouseId, command.LocationId, command.BatchNo);

            balance.QtyOnHand += command.Qty;
            balance.LastMovementDate = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return Result.Success();
    }
}
