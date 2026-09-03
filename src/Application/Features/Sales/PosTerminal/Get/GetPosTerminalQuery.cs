namespace ZARI.Application.Features.Sales.PosTerminals.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPosTerminalQuery(Guid Id) : IQuery<Result<PosTerminalResponse>>;

public sealed record PosTerminalResponse(
    Guid Id,
    string BranchId,
    string Code,
    string Name,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    string Status,
    DateTimeOffset CreatedAt);
