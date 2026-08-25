namespace ZARI.Application.Features.SystemModule.Branches.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Domain.Common;

public sealed class GetAllBranchesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllBranchesQuery, Result<List<BranchResponse>>>
{
    public async Task<Result<List<BranchResponse>>> HandleAsync(GetAllBranchesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("BRANCHES", FormAction.View, cancellationToken))
            return Result.Failure<List<BranchResponse>>(Error.Forbidden("Branch.Forbidden", "You do not have permission to view branches."));

        var branches = await dbContext.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse(b.Id, b.Name, b.Code, b.City, b.Address, b.Phone, b.Status, b.IsHeadOffice))
            .ToListAsync(cancellationToken);

        return Result.Success(branches);
    }
}
