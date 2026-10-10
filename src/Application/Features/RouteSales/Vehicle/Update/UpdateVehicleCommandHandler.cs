namespace ZARI.Application.Features.RouteSales.Vehicles.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Shared;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Domain.Common;

public sealed class UpdateVehicleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateVehicleCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.WithDetails().FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (vehicle is null)
            return Result.Failure(Error.NotFound("Vehicle.NotFound", $"Vehicle with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("VEHICLES", FormAction.Edit, vehicle.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Vehicle.Forbidden", "You do not have permission to update vehicles for this branch."));

        if (await dbContext.Vehicles.AnyAsync(v => v.Id != command.Id && v.PlateNo == command.PlateNo, cancellationToken))
            return Result.Failure(Error.Conflict("Vehicle.DuplicatePlateNo", $"A vehicle with plate no. '{command.PlateNo}' already exists."));

        var terminal = vehicle.PosTerminal;
        var nextNumber = command.OrNextNumber ?? terminal.OrNextNumber;
        var padding = command.OrPaddingLength ?? terminal.OrPaddingLength;
        var seriesChanged = command.OrPrefix != terminal.OrPrefix || nextNumber != terminal.OrNextNumber || padding != terminal.OrPaddingLength;
        if (seriesChanged)
        {
            if (await dbContext.SalesInvoices.AnyAsync(i => i.PosTerminalId == terminal.Id && i.BirOrSeriesNumber != null, cancellationToken))
                return Result.Failure(Error.Conflict("Vehicle.SeriesLocked",
                    "This truck has already issued invoice numbers — its invoice series can no longer be changed."));
            if (await PosTerminalSeriesRules.CheckPrefixUniqueAsync(dbContext, terminal.Id, command.OrPrefix, cancellationToken) is { } prefixError)
                return Result.Failure(prefixError);
        }

        vehicle.PlateNo = command.PlateNo;
        vehicle.Description = command.Description;
        vehicle.Status = command.Status;

        vehicle.Warehouse.Name = $"Truck {command.PlateNo}";
        vehicle.Warehouse.Status = command.Status;

        terminal.Name = $"Truck {command.PlateNo} handheld";
        terminal.Status = command.Status;
        terminal.OrPrefix = command.OrPrefix;
        terminal.OrNextNumber = nextNumber;
        terminal.OrPaddingLength = padding;
        terminal.MachineIdentificationNumber = command.MachineIdentificationNumber;
        terminal.MachineSerialNumber = command.MachineSerialNumber;
        terminal.PosPermitNumber = command.PosPermitNumber;
        terminal.PosPermitDateIssued = command.PosPermitDateIssued;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
