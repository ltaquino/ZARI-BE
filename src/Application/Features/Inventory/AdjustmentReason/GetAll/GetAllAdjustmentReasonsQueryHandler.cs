namespace ZARI.Application.Features.Inventory.AdjustmentReasons.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Domain.Common;

public sealed class GetAllAdjustmentReasonsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllAdjustmentReasonsQuery, Result<List<AdjustmentReasonResponse>>>
{
    public async Task<Result<List<AdjustmentReasonResponse>>> HandleAsync(GetAllAdjustmentReasonsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.View, cancellationToken))
            return Result.Failure<List<AdjustmentReasonResponse>>(Error.Forbidden("AdjustmentReason.Forbidden", "You do not have permission to view adjustment reasons."));

        var items = await dbContext.AdjustmentReasons.AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new AdjustmentReasonResponse(r.Id, r.Code, r.Description, r.GlAccountId, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
