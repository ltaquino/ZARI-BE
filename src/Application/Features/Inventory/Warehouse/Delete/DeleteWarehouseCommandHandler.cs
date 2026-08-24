namespace ZARI.Application.Features.Inventory.Warehouses.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteWarehouseCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteWarehouseCommand>
{
    public async Task<Result> HandleAsync(DeleteWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await dbContext.Warehouses.FindAsync([command.Id], cancellationToken);
        if (warehouse is null)
            return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.Id}' was not found."));

        var locationCount = await dbContext.StorageLocations.CountAsync(l => l.WarehouseId == command.Id, cancellationToken);
        if (locationCount > 0)
            return Result.Failure(Error.Conflict("Warehouse.HasStorageLocations", $"Cannot delete this warehouse — it has {locationCount} storage location{(locationCount == 1 ? "" : "s")}."));

        var settingCount = await dbContext.ItemBranchSettings.CountAsync(s => s.DefaultWarehouseId == command.Id, cancellationToken);
        if (settingCount > 0)
            return Result.Failure(Error.Conflict("Warehouse.InUse", $"Cannot delete this warehouse — it is the default warehouse for {settingCount} reorder setting{(settingCount == 1 ? "" : "s")}."));

        var reservationCount = await dbContext.StockReservations.CountAsync(r => r.WarehouseId == command.Id, cancellationToken);
        if (reservationCount > 0)
            return Result.Failure(Error.Conflict("Warehouse.HasReservations", $"Cannot delete this warehouse — it has {reservationCount} stock reservation{(reservationCount == 1 ? "" : "s")}."));

        dbContext.Warehouses.Remove(warehouse);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
