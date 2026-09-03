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
    string Status) : ICommand<Result<PosTerminalResponse>>;
