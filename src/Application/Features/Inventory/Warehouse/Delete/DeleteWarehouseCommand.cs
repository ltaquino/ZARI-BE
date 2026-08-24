namespace ZARI.Application.Features.Inventory.Warehouses.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteWarehouseCommand(Guid Id) : ICommand;
