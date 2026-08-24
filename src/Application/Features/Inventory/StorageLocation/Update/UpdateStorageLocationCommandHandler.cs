namespace ZARI.Application.Features.Inventory.StorageLocations.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateStorageLocationCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateStorageLocationCommand>
{
    public async Task<Result> HandleAsync(UpdateStorageLocationCommand command, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.StorageLocations.FindAsync([command.Id], cancellationToken);
        if (location is null)
            return Result.Failure(Error.NotFound("StorageLocation.NotFound", $"Storage location with ID '{command.Id}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        location.WarehouseId = command.WarehouseId;
        location.Zone = command.Zone;
        location.Aisle = command.Aisle;
        location.Rack = command.Rack;
        location.BinCode = command.BinCode;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
