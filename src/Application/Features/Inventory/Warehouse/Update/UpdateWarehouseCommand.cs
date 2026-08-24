namespace ZARI.Application.Features.Inventory.Warehouses.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateWarehouseCommand(Guid Id, string BranchId, string Code, string Name, string WarehouseType, string Status) : ICommand;
