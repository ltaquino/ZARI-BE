namespace ZARI.Application.Features.Inventory.StockAdjustments.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockAdjustmentsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllStockAdjustmentsQuery, Result<List<StockAdjustmentResponse>>>
{
    public async Task<Result<List<StockAdjustmentResponse>>> HandleAsync(GetAllStockAdjustmentsQuery query, CancellationToken cancellationToken = default)
    {
        var adjustments = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .OrderByDescending(a => a.AdjustmentDate)
            .ToListAsync(cancellationToken);

        return Result.Success(adjustments.Select(StockAdjustmentMapper.ToResponse).ToList());
    }
}
