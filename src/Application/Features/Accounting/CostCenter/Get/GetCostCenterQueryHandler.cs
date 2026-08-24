namespace ZARI.Application.Features.Accounting.CostCenters.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCostCenterQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetCostCenterQuery, Result<CostCenterResponse>>
{
    public async Task<Result<CostCenterResponse>> HandleAsync(GetCostCenterQuery query, CancellationToken cancellationToken = default)
    {
        var costCenter = await dbContext.CostCenters
            .Where(c => c.Id == query.Id)
            .Select(c => new CostCenterResponse(c.Id, c.BranchId, c.Code, c.Name, c.Status, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (costCenter is null)
            return Result.Failure<CostCenterResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{query.Id}' was not found."));

        return Result.Success(costCenter);
    }
}
