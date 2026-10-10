namespace ZARI.Application.Features.RouteSales.Trips.Update;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateTripCommandHandler(IAppDbContext dbContext, UserManager<ApplicationUser> userManager, IPermissionService permissionService)
    : ICommandHandler<UpdateTripCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(UpdateTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.Include(t => t.Lines).Include(t => t.SalesOrders).FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Edit, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to update trips for this branch."));

        if (trip.Status != TripStatus.Draft)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotDraft", "Only a draft trip can be changed."));

        var plan = await TripPlanning.BuildAsync(dbContext, userManager, trip.BranchId, command, trip.Id, cancellationToken);
        if (!plan.IsSuccess)
            return Result.Failure<TripResponse>(plan.Error!);

        trip.VehicleId = command.VehicleId;
        trip.RouteId = command.RouteId;
        trip.SourceWarehouseId = command.SourceWarehouseId;
        trip.DriverUserId = command.DriverUserId;
        trip.DriverEmployeeId = plan.Value!.DriverEmployeeId;
        trip.HelperNames = command.HelperNames;
        trip.TripDate = command.TripDate;
        trip.Remarks = command.Remarks;

        // Old rows go in their own save: (TripId, ItemId) / (TripId, SalesOrderId) are unique.
        dbContext.TripLines.RemoveRange(trip.Lines);
        dbContext.TripSalesOrders.RemoveRange(trip.SalesOrders);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in plan.Value.Lines)
        {
            line.TripId = trip.Id;
            dbContext.TripLines.Add(line);
        }
        foreach (var so in plan.Value.SalesOrders)
        {
            so.TripId = trip.Id;
            dbContext.TripSalesOrders.Add(so);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}
