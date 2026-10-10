namespace ZARI.Application.Features.RouteSales.Vehicles.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Application.Features.RouteSales.Vehicles.Shared;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateVehicleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateVehicleCommand, Result<VehicleResponse>>
{
    public const string WarehouseCodePrefix = "TRK-";

    public async Task<Result<VehicleResponse>> HandleAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("VEHICLES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Forbidden("Vehicle.Forbidden", "You do not have permission to create vehicles for this branch."));

        if (!await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.NotFound("Vehicle.BranchNotFound", $"Branch '{command.BranchId}' was not found."));

        if (await dbContext.Vehicles.AnyAsync(v => v.BranchId == command.BranchId && v.Code == command.Code, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Conflict("Vehicle.DuplicateCode", $"A vehicle with code '{command.Code}' already exists at this branch."));

        if (await dbContext.Vehicles.AnyAsync(v => v.PlateNo == command.PlateNo, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Conflict("Vehicle.DuplicatePlateNo", $"A vehicle with plate no. '{command.PlateNo}' already exists."));

        var warehouseCode = WarehouseCodePrefix + command.Code;
        if (await dbContext.Warehouses.AnyAsync(w => w.Code == warehouseCode, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Conflict("Vehicle.WarehouseCodeTaken", $"Warehouse code '{warehouseCode}' is already in use."));

        if (await dbContext.PosTerminals.AnyAsync(t => t.BranchId == command.BranchId && t.Code == command.Code, cancellationToken))
            return Result.Failure<VehicleResponse>(Error.Conflict("Vehicle.TerminalCodeTaken", $"A POS terminal with code '{command.Code}' already exists at this branch."));

        if (await PosTerminalSeriesRules.CheckPrefixUniqueAsync(dbContext, null, command.OrPrefix, cancellationToken) is { } prefixError)
            return Result.Failure<VehicleResponse>(prefixError);

        var warehouse = new Warehouse
        {
            BranchId = command.BranchId,
            Code = warehouseCode,
            Name = $"Truck {command.PlateNo}",
            WarehouseType = "Vehicle",
            Status = command.Status,
        };
        var terminal = new PosTerminal
        {
            BranchId = command.BranchId,
            Code = command.Code,
            Name = $"Truck {command.PlateNo} handheld",
            MachineIdentificationNumber = command.MachineIdentificationNumber,
            MachineSerialNumber = command.MachineSerialNumber,
            PosPermitNumber = command.PosPermitNumber,
            PosPermitDateIssued = command.PosPermitDateIssued,
            Status = command.Status,
            HasOwnSeries = true,
            OrPrefix = command.OrPrefix,
            OrNextNumber = command.OrNextNumber ?? 1,
            OrPaddingLength = command.OrPaddingLength ?? 6,
        };
        var vehicle = new Vehicle
        {
            BranchId = command.BranchId,
            Code = command.Code,
            PlateNo = command.PlateNo,
            Description = command.Description,
            Status = command.Status,
            Warehouse = warehouse,
            PosTerminal = terminal,
        };

        // One SaveChanges: the vehicle, its warehouse and its terminal exist together or not at all.
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleMapper.ToResponse(vehicle));
    }
}
