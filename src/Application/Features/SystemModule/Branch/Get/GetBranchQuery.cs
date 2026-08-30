namespace ZARI.Application.Features.SystemModule.Branches.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetBranchQuery(string Id) : IQuery<Result<BranchResponse>>;

public sealed record BranchResponse(
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
    string? MachineSerialNumber);
