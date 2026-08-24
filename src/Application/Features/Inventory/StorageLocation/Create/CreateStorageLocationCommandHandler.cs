namespace ZARI.Application.Features.Inventory.StorageLocations.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStorageLocationCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateStorageLocationCommand, Result<StorageLocationResponse>>
{
    public async Task<Result<StorageLocationResponse>> HandleAsync(CreateStorageLocationCommand command, CancellationToken cancellationToken = default)
    {
        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StorageLocationResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var location = new StorageLocation
        {
            WarehouseId = command.WarehouseId,
            Zone = command.Zone,
            Aisle = command.Aisle,
            Rack = command.Rack,
            BinCode = command.BinCode
        };

        dbContext.StorageLocations.Add(location);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new StorageLocationResponse(location.Id, location.WarehouseId, location.Zone, location.Aisle, location.Rack, location.BinCode, location.CreatedAt);
        return Result.Success(response);
    }
}
