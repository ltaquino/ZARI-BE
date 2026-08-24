namespace ZARI.Application.Features.Inventory.StorageLocations.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateStorageLocationCommand(Guid Id, Guid WarehouseId, string? Zone, string? Aisle, string? Rack, string? BinCode) : ICommand;
