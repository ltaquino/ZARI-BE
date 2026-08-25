namespace ZARI.Application.Features.Inventory.Warehouses.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetWarehouseQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetWarehouseQuery, Result<WarehouseResponse>>
{
    public async Task<Result<WarehouseResponse>> HandleAsync(GetWarehouseQuery query, CancellationToken cancellationToken = default)
    {
        var warehouse = await dbContext.Warehouses
            .Where(w => w.Id == query.Id)
            .Select(w => new WarehouseResponse(w.Id, w.BranchId, w.Code, w.Name, w.WarehouseType, w.Status, w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null)
            return Result.Failure<WarehouseResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("WAREHOUSES", FormAction.View, warehouse.BranchId, cancellationToken))
            return Result.Failure<WarehouseResponse>(Error.Forbidden("Warehouse.Forbidden", "You do not have permission to view warehouses for this branch."));

        return Result.Success(warehouse);
    }
}
