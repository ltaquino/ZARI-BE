namespace ZARI.Application.Features.RouteSales.Trips.Create;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateTripCommandHandler(
    IAppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateTripCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(CreateTripCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to plan trips for this branch."));

        if (!await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.NotFound("Branch.NotFound", $"Branch '{command.BranchId}' was not found."));

        var plan = await TripPlanning.BuildAsync(dbContext, userManager, command.BranchId, command, null, cancellationToken);
        if (!plan.IsSuccess)
            return Result.Failure<TripResponse>(plan.Error!);

        var number = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "TRIP"), cancellationToken);
        if (!number.IsSuccess)
            return Result.Failure<TripResponse>(number.Error!);

        var trip = new Trip
        {
            TripNo = number.Value!.DocumentNumber,
            BranchId = command.BranchId,
            VehicleId = command.VehicleId,
            RouteId = command.RouteId,
            SourceWarehouseId = command.SourceWarehouseId,
            DriverUserId = command.DriverUserId,
            DriverEmployeeId = plan.Value!.DriverEmployeeId,
            HelperNames = command.HelperNames,
            TripDate = command.TripDate,
            Remarks = command.Remarks,
            Status = TripStatus.Draft,
            CreatedBy = command.CreatedBy,
            Lines = plan.Value.Lines,
            SalesOrders = plan.Value.SalesOrders,
        };

        dbContext.Trips.Add(trip);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}
