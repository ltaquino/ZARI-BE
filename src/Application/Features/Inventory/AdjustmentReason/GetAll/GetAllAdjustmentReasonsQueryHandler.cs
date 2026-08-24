namespace ZARI.Application.Features.Inventory.AdjustmentReasons.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Domain.Common;

public sealed class GetAllAdjustmentReasonsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllAdjustmentReasonsQuery, Result<List<AdjustmentReasonResponse>>>
{
    public async Task<Result<List<AdjustmentReasonResponse>>> HandleAsync(GetAllAdjustmentReasonsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.AdjustmentReasons
            .OrderBy(r => r.Code)
            .Select(r => new AdjustmentReasonResponse(r.Id, r.Code, r.Description, r.GlAccountId, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
