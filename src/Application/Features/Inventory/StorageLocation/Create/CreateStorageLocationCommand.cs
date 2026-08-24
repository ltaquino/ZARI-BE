namespace ZARI.Application.Features.Inventory.StorageLocations.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Domain.Common;

public sealed record CreateStorageLocationCommand(Guid WarehouseId, string? Zone, string? Aisle, string? Rack, string? BinCode) : ICommand<Result<StorageLocationResponse>>;
