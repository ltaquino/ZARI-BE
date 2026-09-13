namespace ZARI.Application.Features.Inventory.StockLocationBalances.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllStockLocationBalancesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockLocationBalancesQuery, Result<List<StockLocationBalanceResponse>>>
{
    public async Task<Result<List<StockLocationBalanceResponse>>> HandleAsync(GetAllStockLocationBalancesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEMS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockLocationBalanceResponse>>(Error.Forbidden("StockLocationBalance.Forbidden", "You do not have permission to view stock location balances."));

        var items = await dbContext.StockLocationBalances.AsNoTracking()
            .Where(b => b.QtyOnHand > 0.0001m)
            .Select(b => new StockLocationBalanceResponse(b.Id, b.ItemId, b.WarehouseId, b.LocationId, b.BatchNo, b.QtyOnHand, b.LastMovementDate))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
