namespace ZARI.Application.Features.RouteSales.Vehicles.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Deletes the vehicle with its warehouse and terminal — only while nothing has happened on it yet.
/// Once the truck has stock history or has issued an invoice, those records must stay traceable to
/// it, so it can only be set to inactive.
/// </summary>
public sealed class DeleteVehicleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteVehicleCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.Include(v => v.Warehouse).Include(v => v.PosTerminal)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (vehicle is null)
            return Result.Failure(Error.NotFound("Vehicle.NotFound", $"Vehicle with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("VEHICLES", FormAction.Delete, vehicle.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Vehicle.Forbidden", "You do not have permission to delete vehicles for this branch."));

        if (await dbContext.StockLedgers.AnyAsync(l => l.WarehouseId == vehicle.WarehouseId, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.HasStockHistory", "This truck has stock history and cannot be deleted — set it to inactive instead."));

        if (await dbContext.SalesInvoices.AnyAsync(i => i.PosTerminalId == vehicle.PosTerminalId, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.HasInvoices", "This truck has issued invoices and cannot be deleted — set it to inactive instead."));

        // A Z-Reading is a permanent BIR record of this handheld's closings — it must stay attached.
        if (await dbContext.ZReadings.AnyAsync(z => z.PosTerminalId == vehicle.PosTerminalId, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.HasZReadings", "This truck has Z-Reading closings and cannot be deleted — set it to inactive instead."));

        if (await dbContext.Trips.AnyAsync(t => t.VehicleId == vehicle.Id, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.HasTrips", "This truck has trips and cannot be deleted — set it to inactive instead."));

        if (await dbContext.DeliveryRoutes.AnyAsync(r => r.DefaultVehicleId == vehicle.Id, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.UsedByRoute", "This truck is the default vehicle of a route — change the route first."));

        dbContext.Vehicles.Remove(vehicle);
        dbContext.Warehouses.Remove(vehicle.Warehouse);
        dbContext.PosTerminals.Remove(vehicle.PosTerminal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
