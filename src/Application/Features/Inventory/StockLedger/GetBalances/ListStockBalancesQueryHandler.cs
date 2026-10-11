namespace ZARI.Application.Features.Inventory.StockLedgers.GetBalances;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class ListStockBalancesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<ListStockBalancesQuery, Result<List<StockBalanceResponse>>>
{
    public async Task<Result<List<StockBalanceResponse>>> HandleAsync(ListStockBalancesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockBalanceResponse>>(Error.Forbidden("StockBalance.Forbidden", "You do not have permission to view stock balances."));

        var source = dbContext.StockBalances.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.BranchId))
            source = source.Where(b => b.BranchId == query.BranchId);
        if (query.UpdatedSince is { } since)
            source = source.Where(b => b.CreatedAt >= since || b.LastModifiedAt >= since);

        var balances = await source
            .OrderByDescending(b => b.LastMovementDate)
            .Select(b => new StockBalanceResponse(b.Id, b.ItemId, b.BranchId, b.WarehouseId, b.BatchNo, b.QtyOnHand, b.AvgUnitCost, b.TotalValue, b.LastMovementDate))
            .ToListAsync(cancellationToken);

        return Result.Success(balances);
    }
}
