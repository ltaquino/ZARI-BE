namespace ZARI.Application.Features.Inventory.StorageLocations.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Domain.Common;

public sealed class GetAllStorageLocationsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllStorageLocationsQuery, Result<List<StorageLocationResponse>>>
{
    public async Task<Result<List<StorageLocationResponse>>> HandleAsync(GetAllStorageLocationsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.StorageLocations
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Zone).ThenBy(l => l.Aisle).ThenBy(l => l.Rack).ThenBy(l => l.BinCode)
            .Select(l => new StorageLocationResponse(l.Id, l.WarehouseId, l.Zone, l.Aisle, l.Rack, l.BinCode, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
