namespace ZARI.Application.Features.RouteSales.Vehicles.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Domain.Entities;

internal static class VehicleMapper
{
    /// <summary>Callers must load Warehouse and PosTerminal (see <see cref="WithDetails"/>).</summary>
    public static VehicleResponse ToResponse(Vehicle v) => new(
        v.Id, v.BranchId, v.Code, v.PlateNo, v.Description, v.Status,
        v.WarehouseId, v.Warehouse.Code, v.PosTerminalId, v.PosTerminal.Code,
        v.PosTerminal.OrPrefix, v.PosTerminal.OrNextNumber, v.PosTerminal.OrPaddingLength,
        v.PosTerminal.MachineIdentificationNumber, v.PosTerminal.MachineSerialNumber,
        v.PosTerminal.PosPermitNumber, v.PosTerminal.PosPermitDateIssued, v.CreatedAt);

    public static IQueryable<Vehicle> WithDetails(this IQueryable<Vehicle> query) =>
        query.Include(v => v.Warehouse).Include(v => v.PosTerminal);
}
