namespace ZARI.Application.Features.Sales.PosTerminals.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePosTerminalCommand(
    Guid Id,
    string Code,
    string Name,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    string Status) : ICommand;
