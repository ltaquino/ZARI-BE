namespace ZARI.Application.Features.Inventory.StockAdjustments.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockAdjustmentsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockAdjustmentsPagedQuery, Result<PagedResult<StockAdjustmentResponse>>>
{
    public async Task<Result<PagedResult<StockAdjustmentResponse>>> HandleAsync(GetAllStockAdjustmentsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_ADJUSTMENTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<StockAdjustmentResponse>>(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to view stock adjustments."));

        var baseQuery = dbContext.StockAdjustments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.AdjustmentNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var adjustments = await baseQuery
            .OrderByDescending(a => a.AdjustmentDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockAdjustmentResponse>(adjustments.Select(StockAdjustmentMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
