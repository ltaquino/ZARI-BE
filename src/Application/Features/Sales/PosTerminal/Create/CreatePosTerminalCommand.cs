namespace ZARI.Application.Features.Sales.PosTerminals.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Domain.Common;

public sealed record CreatePosTerminalCommand(
    string BranchId,
    string Code,
    string Name,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    string Status,
    // Own BIR invoice series — Route Sales truck handhelds only (see PosTerminal). Normally set by
    // CreateVehicleCommandHandler rather than directly.
    bool HasOwnSeries = false,
    string? OrPrefix = null,
    int? OrNextNumber = null,
    int? OrPaddingLength = null) : ICommand<Result<PosTerminalResponse>>;
