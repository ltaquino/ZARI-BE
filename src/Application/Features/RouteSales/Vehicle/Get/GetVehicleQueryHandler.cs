namespace ZARI.Application.Features.RouteSales.Vehicles.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Shared;
using ZARI.Domain.Common;

public sealed class GetVehicleQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetVehicleQuery, Result<VehicleResponse>>
{
    public async Task<Result<VehicleResponse>> HandleAsync(GetVehicleQuery query, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().WithDetails().FirstOrDefaultAsync(v => v.Id == query.Id, cancellationToken);
        if (vehicle is null)
            return Result.Failure<VehicleResponse>(Error.NotFound("Vehicle.NotFound", $"Vehicle with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("VEHICLES", FormAction.View, vehicle.BranchId, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Forbidden("Vehicle.Forbidden", "You do not have permission to view vehicles for this branch."));

        return Result.Success(VehicleMapper.ToResponse(vehicle));
    }
}
