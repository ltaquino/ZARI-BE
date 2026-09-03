namespace ZARI.Application.Features.Inventory.StockAdjustments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockAdjustmentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockAdjustmentsQuery, Result<List<StockAdjustmentResponse>>>
{
    public async Task<Result<List<StockAdjustmentResponse>>> HandleAsync(GetAllStockAdjustmentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_ADJUSTMENTS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockAdjustmentResponse>>(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to view stock adjustments."));

        var adjustments = await dbContext.StockAdjustments.AsNoTracking()
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .OrderByDescending(a => a.AdjustmentDate)
            .ToListAsync(cancellationToken);

        return Result.Success(adjustments.Select(StockAdjustmentMapper.ToResponse).ToList());
    }
}
