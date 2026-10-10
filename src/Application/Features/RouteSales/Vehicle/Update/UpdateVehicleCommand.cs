namespace ZARI.Application.Features.RouteSales.Vehicles.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Code (and so the warehouse/terminal codes derived from it) and branch are fixed after creation.
/// Status cascades to the vehicle's warehouse and terminal. The invoice series follows the same lock
/// as UpdatePosTerminalCommandHandler: editable only until the handheld has issued a number.
/// </summary>
public sealed record UpdateVehicleCommand(
    Guid Id,
    string PlateNo,
    string? Description,
    string Status,
    string OrPrefix,
    int? OrNextNumber = null,
    int? OrPaddingLength = null,
    string? MachineIdentificationNumber = null,
    string? MachineSerialNumber = null,
    string? PosPermitNumber = null,
    DateTime? PosPermitDateIssued = null) : ICommand;
