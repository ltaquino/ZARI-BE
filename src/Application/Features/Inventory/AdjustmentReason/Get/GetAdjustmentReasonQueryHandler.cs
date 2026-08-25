namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAdjustmentReasonQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAdjustmentReasonQuery, Result<AdjustmentReasonResponse>>
{
    public async Task<Result<AdjustmentReasonResponse>> HandleAsync(GetAdjustmentReasonQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.View, cancellationToken))
            return Result.Failure<AdjustmentReasonResponse>(Error.Forbidden("AdjustmentReason.Forbidden", "You do not have permission to view adjustment reasons."));

        var reason = await dbContext.AdjustmentReasons
            .Where(r => r.Id == query.Id)
            .Select(r => new AdjustmentReasonResponse(r.Id, r.Code, r.Description, r.GlAccountId, r.Status, r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (reason is null)
            return Result.Failure<AdjustmentReasonResponse>(Error.NotFound("AdjustmentReason.NotFound", $"Adjustment reason with ID '{query.Id}' was not found."));

        return Result.Success(reason);
    }
}
