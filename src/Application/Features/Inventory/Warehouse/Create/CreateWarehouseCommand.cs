namespace ZARI.Application.Features.Inventory.Warehouses.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Domain.Common;

public sealed record CreateWarehouseCommand(string BranchId, string Code, string Name, string WarehouseType, string Status) : ICommand<Result<WarehouseResponse>>;
