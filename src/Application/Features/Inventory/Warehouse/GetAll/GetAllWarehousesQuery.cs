namespace ZARI.Application.Features.Inventory.Warehouses.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Domain.Common;

public sealed record GetAllWarehousesQuery : IQuery<Result<List<WarehouseResponse>>>;
