namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Domain.Common;

public sealed class GetDeliveryRouteQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetDeliveryRouteQuery, Result<DeliveryRouteResponse>>
{
    public async Task<Result<DeliveryRouteResponse>> HandleAsync(GetDeliveryRouteQuery query, CancellationToken cancellationToken = default)
    {
        var route = await dbContext.DeliveryRoutes.AsNoTracking().WithDetails().FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);
        if (route is null)
            return Result.Failure<DeliveryRouteResponse>(Error.NotFound("DeliveryRoute.NotFound", $"Route with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("ROUTES", FormAction.View, route.BranchId, cancellationToken))
            return Result.Failure<DeliveryRouteResponse>(Error.Forbidden("DeliveryRoute.Forbidden", "You do not have permission to view routes for this branch."));

        return Result.Success(DeliveryRouteShared.ToResponse(route));
    }
}
