namespace ZARI.Application.Features.Inventory.Warehouses.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Domain.Common;

public sealed class GetAllWarehousesQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllWarehousesQuery, Result<List<WarehouseResponse>>>
{
    public async Task<Result<List<WarehouseResponse>>> HandleAsync(GetAllWarehousesQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Warehouses
            .OrderBy(w => w.Code)
            .Select(w => new WarehouseResponse(w.Id, w.BranchId, w.Code, w.Name, w.WarehouseType, w.Status, w.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
