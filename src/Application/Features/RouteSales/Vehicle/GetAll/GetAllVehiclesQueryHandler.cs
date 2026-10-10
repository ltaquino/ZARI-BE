namespace ZARI.Application.Features.RouteSales.Vehicles.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Application.Features.RouteSales.Vehicles.Shared;
using ZARI.Domain.Common;

public sealed class GetAllVehiclesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllVehiclesQuery, Result<List<VehicleResponse>>>
{
    public async Task<Result<List<VehicleResponse>>> HandleAsync(GetAllVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.Vehicles.AsNoTracking().WithDetails()
            .Where(v => query.BranchId == null || v.BranchId == query.BranchId)
            .OrderBy(v => v.BranchId).ThenBy(v => v.Code)
            .ToListAsync(cancellationToken);

        var results = new List<VehicleResponse>();
        foreach (var vehicle in candidates)
        {
            if (await permissionService.HasPermissionOnBranchAsync("VEHICLES", FormAction.View, vehicle.BranchId, cancellationToken))
                results.Add(VehicleMapper.ToResponse(vehicle));
        }

        return Result.Success(results);
    }
}
