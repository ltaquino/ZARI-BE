namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDeliveryRouteCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteDeliveryRouteCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteDeliveryRouteCommand command, CancellationToken cancellationToken = default)
    {
        var route = await dbContext.DeliveryRoutes.FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (route is null)
            return Result.Failure(Error.NotFound("DeliveryRoute.NotFound", $"Route with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("ROUTES", FormAction.Delete, route.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DeliveryRoute.Forbidden", "You do not have permission to delete routes for this branch."));

        if (await dbContext.Trips.AnyAsync(t => t.RouteId == route.Id, cancellationToken))
            return Result.Failure(Error.Conflict("DeliveryRoute.UsedByTrip", "This route has trips — set it to inactive instead."));

        // Stops cascade.
        dbContext.DeliveryRoutes.Remove(route);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
