namespace ZARI.Application.Features.Inventory.StorageLocations.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetStorageLocationQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetStorageLocationQuery, Result<StorageLocationResponse>>
{
    public async Task<Result<StorageLocationResponse>> HandleAsync(GetStorageLocationQuery query, CancellationToken cancellationToken = default)
    {
        var location = await dbContext.StorageLocations
            .Where(l => l.Id == query.Id)
            .Select(l => new StorageLocationResponse(l.Id, l.WarehouseId, l.Zone, l.Aisle, l.Rack, l.BinCode, l.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
            return Result.Failure<StorageLocationResponse>(Error.NotFound("StorageLocation.NotFound", $"Storage location with ID '{query.Id}' was not found."));

        return Result.Success(location);
    }
}
