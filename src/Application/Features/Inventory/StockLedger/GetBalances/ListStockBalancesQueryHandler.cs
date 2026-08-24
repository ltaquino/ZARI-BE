namespace ZARI.Application.Features.Inventory.StockLedgers.GetBalances;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class ListStockBalancesQueryHandler(IAppDbContext dbContext) : IQueryHandler<ListStockBalancesQuery, Result<List<StockBalanceResponse>>>
{
    public async Task<Result<List<StockBalanceResponse>>> HandleAsync(ListStockBalancesQuery query, CancellationToken cancellationToken = default)
    {
        var balances = await dbContext.StockBalances
            .OrderByDescending(b => b.LastMovementDate)
            .Select(b => new StockBalanceResponse(b.Id, b.ItemId, b.BranchId, b.WarehouseId, b.BatchNo, b.QtyOnHand, b.AvgUnitCost, b.TotalValue, b.LastMovementDate))
            .ToListAsync(cancellationToken);

        return Result.Success(balances);
    }
}
