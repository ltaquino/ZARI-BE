namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Domain.Common;

public sealed class GetAllDeliveryRoutesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllDeliveryRoutesQuery, Result<List<DeliveryRouteResponse>>>
{
    public async Task<Result<List<DeliveryRouteResponse>>> HandleAsync(GetAllDeliveryRoutesQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.DeliveryRoutes.AsNoTracking().WithDetails()
            .Where(r => query.BranchId == null || r.BranchId == query.BranchId)
            .OrderBy(r => r.BranchId).ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);

        var results = new List<DeliveryRouteResponse>();
        foreach (var route in candidates)
        {
            if (await permissionService.HasPermissionOnBranchAsync("ROUTES", FormAction.View, route.BranchId, cancellationToken))
                results.Add(DeliveryRouteShared.ToResponse(route));
        }

        return Result.Success(results);
    }
}
