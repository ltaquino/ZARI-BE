namespace ZARI.Application.Features.Accounting.CostCenters.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Domain.Common;

public sealed class GetAllCostCentersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllCostCentersQuery, Result<List<CostCenterResponse>>>
{
    public async Task<Result<List<CostCenterResponse>>> HandleAsync(GetAllCostCentersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("COST_CENTERS", FormAction.View, cancellationToken))
            return Result.Failure<List<CostCenterResponse>>(Error.Forbidden("CostCenter.Forbidden", "You do not have permission to view cost centers."));

        var items = await dbContext.CostCenters.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CostCenterResponse(c.Id, c.BranchId, c.Code, c.Name, c.Status, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
