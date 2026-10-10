namespace ZARI.Application.Features.RouteSales.Vehicles.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Vehicles.Get;
using ZARI.Domain.Common;

/// <summary>
/// Creates the vehicle together with its Vehicle-type warehouse (code "TRK-{Code}") and its handheld
/// PosTerminal (code = vehicle code) with its own BIR invoice series. OrPrefix/OrNextNumber come from
/// the truck's BIR permit; MIN/serial/permit are the handheld's registration details.
/// </summary>
public sealed record CreateVehicleCommand(
    string BranchId,
    string Code,
    string PlateNo,
    string? Description,
    string Status,
    string OrPrefix,
    int? OrNextNumber = null,
    int? OrPaddingLength = null,
    string? MachineIdentificationNumber = null,
    string? MachineSerialNumber = null,
    string? PosPermitNumber = null,
    DateTime? PosPermitDateIssued = null) : ICommand<Result<VehicleResponse>>;
