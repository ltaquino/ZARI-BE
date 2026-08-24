namespace ZARI.Application.Features.SystemModule.Branches.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Domain.Common;

public sealed record GetAllBranchesQuery : IQuery<Result<List<BranchResponse>>>;
