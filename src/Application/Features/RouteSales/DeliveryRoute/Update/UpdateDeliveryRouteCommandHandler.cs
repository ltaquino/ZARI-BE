namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Domain.Common;

public sealed class UpdateDeliveryRouteCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateDeliveryRouteCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateDeliveryRouteCommand command, CancellationToken cancellationToken = default)
    {
        var route = await dbContext.DeliveryRoutes.Include(r => r.Stops).FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (route is null)
            return Result.Failure(Error.NotFound("DeliveryRoute.NotFound", $"Route with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("ROUTES", FormAction.Edit, route.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DeliveryRoute.Forbidden", "You do not have permission to update routes for this branch."));

        if (await dbContext.DeliveryRoutes.AnyAsync(r => r.Id != command.Id && r.BranchId == route.BranchId && r.Code == command.Code, cancellationToken))
            return Result.Failure(Error.Conflict("DeliveryRoute.DuplicateCode", $"A route with code '{command.Code}' already exists at this branch."));

        if (await DeliveryRouteShared.CheckReferencesAsync(dbContext, route.BranchId, command.DefaultVehicleId, command.Stops, cancellationToken) is { } refError)
            return Result.Failure(refError);

        route.Code = command.Code;
        route.Name = command.Name;
        route.Description = command.Description;
        route.DefaultVehicleId = command.DefaultVehicleId;
        route.Status = command.Status;

        // Delete the old stops in their own save first: the (RouteId, Sequence) and (RouteId,
        // CustomerId) unique indexes would otherwise collide with the replacement rows mid-batch.
        dbContext.DeliveryRouteStops.RemoveRange(route.Stops);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var stop in DeliveryRouteShared.BuildStops(command.Stops))
        {
            stop.RouteId = route.Id;
            dbContext.DeliveryRouteStops.Add(stop);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
