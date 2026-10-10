namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDeliveryRouteCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateDeliveryRouteCommand, Result<DeliveryRouteResponse>>
{
    public async Task<Result<DeliveryRouteResponse>> HandleAsync(CreateDeliveryRouteCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("ROUTES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<DeliveryRouteResponse>(Error.Forbidden("DeliveryRoute.Forbidden", "You do not have permission to create routes for this branch."));

        if (!await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken))
            return Result.Failure<DeliveryRouteResponse>(Error.NotFound("DeliveryRoute.BranchNotFound", $"Branch '{command.BranchId}' was not found."));

        if (await dbContext.DeliveryRoutes.AnyAsync(r => r.BranchId == command.BranchId && r.Code == command.Code, cancellationToken))
            return Result.Failure<DeliveryRouteResponse>(Error.Conflict("DeliveryRoute.DuplicateCode", $"A route with code '{command.Code}' already exists at this branch."));

        if (await DeliveryRouteShared.CheckReferencesAsync(dbContext, command.BranchId, command.DefaultVehicleId, command.Stops, cancellationToken) is { } refError)
            return Result.Failure<DeliveryRouteResponse>(refError);

        var route = new DeliveryRoute
        {
            BranchId = command.BranchId,
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            DefaultVehicleId = command.DefaultVehicleId,
            Status = command.Status,
            Stops = DeliveryRouteShared.BuildStops(command.Stops),
        };

        dbContext.DeliveryRoutes.Add(route);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.DeliveryRoutes.AsNoTracking().WithDetails().FirstAsync(r => r.Id == route.Id, cancellationToken);
        return Result.Success(DeliveryRouteShared.ToResponse(saved));
    }
}
