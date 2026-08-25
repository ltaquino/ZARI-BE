namespace ZARI.Application.Features.Accounting.CostCenters.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCostCenterQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetCostCenterQuery, Result<CostCenterResponse>>
{
    public async Task<Result<CostCenterResponse>> HandleAsync(GetCostCenterQuery query, CancellationToken cancellationToken = default)
    {
        var costCenter = await dbContext.CostCenters
            .Where(c => c.Id == query.Id)
            .Select(c => new CostCenterResponse(c.Id, c.BranchId, c.Code, c.Name, c.Status, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (costCenter is null)
            return Result.Failure<CostCenterResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{query.Id}' was not found."));

        var hasPermission = costCenter.BranchId is not null
            ? await permissionService.HasPermissionOnBranchAsync("COST_CENTERS", FormAction.View, costCenter.BranchId, cancellationToken)
            : await permissionService.HasPermissionAsync("COST_CENTERS", FormAction.View, cancellationToken);
        if (!hasPermission)
            return Result.Failure<CostCenterResponse>(Error.Forbidden("CostCenter.Forbidden", "You do not have permission to view this cost center."));

        return Result.Success(costCenter);
    }
}
