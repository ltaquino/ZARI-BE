namespace ZARI.Application.Features.Inventory.StorageLocations.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetStorageLocationQuery(Guid Id) : IQuery<Result<StorageLocationResponse>>;

public sealed record StorageLocationResponse(Guid Id, Guid WarehouseId, string? Zone, string? Aisle, string? Rack, string? BinCode, DateTimeOffset CreatedAt);
