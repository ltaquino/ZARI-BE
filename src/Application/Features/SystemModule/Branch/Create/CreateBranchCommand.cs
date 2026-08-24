namespace ZARI.Application.Features.SystemModule.Branches.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Domain.Common;

public sealed record CreateBranchCommand(
    string Name,
    string Code,
    string City,
    string Address,
    string Phone,
    string Status,
    bool IsHeadOffice) : ICommand<Result<BranchResponse>>;
