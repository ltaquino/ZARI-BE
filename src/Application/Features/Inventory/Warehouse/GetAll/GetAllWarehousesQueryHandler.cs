namespace ZARI.Application.Features.Inventory.Warehouses.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Domain.Common;

public sealed class GetAllWarehousesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllWarehousesQuery, Result<List<WarehouseResponse>>>
{
    public async Task<Result<List<WarehouseResponse>>> HandleAsync(GetAllWarehousesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WAREHOUSES", FormAction.View, cancellationToken))
            return Result.Failure<List<WarehouseResponse>>(Error.Forbidden("Warehouse.Forbidden", "You do not have permission to view warehouses."));

        var items = await dbContext.Warehouses
            .OrderBy(w => w.Code)
            .Select(w => new WarehouseResponse(w.Id, w.BranchId, w.Code, w.Name, w.WarehouseType, w.Status, w.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
