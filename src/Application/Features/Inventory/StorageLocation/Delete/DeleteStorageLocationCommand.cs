namespace ZARI.Application.Features.Inventory.StorageLocations.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteStorageLocationCommand(Guid Id) : ICommand;
