namespace ZARI.Application.Features.Inventory.Warehouses.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetWarehouseQuery(Guid Id) : IQuery<Result<WarehouseResponse>>;

public sealed record WarehouseResponse(Guid Id, string BranchId, string Code, string Name, string WarehouseType, string Status, DateTimeOffset CreatedAt);
