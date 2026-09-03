namespace ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockLocationTransfersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllStockLocationTransfersQuery, Result<List<StockLocationTransferResponse>>>
{
    public async Task<Result<List<StockLocationTransferResponse>>> HandleAsync(GetAllStockLocationTransfersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_LOCATION_TRANSFERS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockLocationTransferResponse>>(Error.Forbidden("StockLocationTransfer.Forbidden", "You do not have permission to view bin transfers."));

        var transfers = await dbContext.StockLocationTransfers.AsNoTracking()
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .OrderByDescending(t => t.TransferDate)
            .ToListAsync(cancellationToken);

        return Result.Success(transfers.Select(StockLocationTransferMapper.ToResponse).ToList());
    }
}
