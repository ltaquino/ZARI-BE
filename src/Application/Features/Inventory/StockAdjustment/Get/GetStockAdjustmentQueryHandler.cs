namespace ZARI.Application.Features.Inventory.StockAdjustments.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Domain.Common;

public sealed class GetStockAdjustmentQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetStockAdjustmentQuery, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(GetStockAdjustmentQuery query, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.View, adjustment.BranchId, cancellationToken))
            return Result.Failure<StockAdjustmentResponse>(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to view stock adjustments for this branch."));

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
