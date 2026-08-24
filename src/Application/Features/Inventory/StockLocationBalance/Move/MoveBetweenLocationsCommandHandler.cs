namespace ZARI.Application.Features.Inventory.StockLocationBalances.Move;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationBalances.Shared;
using ZARI.Domain.Common;

public sealed class MoveBetweenLocationsCommandHandler(IAppDbContext dbContext) : ICommandHandler<MoveBetweenLocationsCommand, Result>
{
    public async Task<Result> HandleAsync(MoveBetweenLocationsCommand command, CancellationToken cancellationToken = default)
    {
        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var lockedRows = await StockLocationBalanceLocker.LockItemWarehouseAsync(dbContext, command.ItemId, command.WarehouseId, cancellationToken);

            var fromBalance = StockLocationBalanceLocker.FindExact(lockedRows, command.ItemId, command.WarehouseId, command.FromLocationId, command.BatchNo);
            var onHand = fromBalance?.QtyOnHand ?? 0;
            if (onHand < command.Qty - 0.0001m)
            {
                return Result.Failure(Error.Validation(
                    "StockLocationBalance.InsufficientQty",
                    $"Insufficient qty at the source location (on hand: {onHand}, requested: {command.Qty})."));
            }

            var now = DateTimeOffset.UtcNow;
            fromBalance!.QtyOnHand -= command.Qty;
            fromBalance.LastMovementDate = now;

            var toBalance = StockLocationBalanceLocker.GetOrCreate(dbContext, lockedRows, command.ItemId, command.WarehouseId, command.ToLocationId, command.BatchNo);
            toBalance.QtyOnHand += command.Qty;
            toBalance.LastMovementDate = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }
}
