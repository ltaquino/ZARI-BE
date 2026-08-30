namespace ZARI.Application.Features.SystemModule.Branches.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetBranchQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetBranchQuery, Result<BranchResponse>>
{
    public async Task<Result<BranchResponse>> HandleAsync(GetBranchQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("BRANCHES", FormAction.View, cancellationToken))
            return Result.Failure<BranchResponse>(Error.Forbidden("Branch.Forbidden", "You do not have permission to view branches."));

        var branch = await dbContext.Branches
            .Where(b => b.Id == query.Id)
            .Select(b => new BranchResponse(
                b.Id, b.Name, b.Code, b.City, b.Address, b.Phone, b.Status, b.IsHeadOffice,
                b.BirBranchCode, b.PosPermitNumber, b.PosPermitDateIssued, b.MachineIdentificationNumber, b.MachineSerialNumber))
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            return Result.Failure<BranchResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{query.Id}' was not found."));

        return Result.Success(branch);
    }
}
