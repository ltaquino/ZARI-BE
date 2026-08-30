namespace ZARI.Application.Features.SystemModule.Branches.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateBranchCommand(
    string Id,
    string Name,
    string Code,
    string City,
    string Address,
    string Phone,
    string Status,
    bool IsHeadOffice,
    string? BirBranchCode,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber) : ICommand;
