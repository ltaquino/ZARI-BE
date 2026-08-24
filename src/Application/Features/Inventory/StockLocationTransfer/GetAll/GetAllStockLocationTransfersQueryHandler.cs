namespace ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockLocationTransfersQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAllStockLocationTransfersQuery, Result<List<StockLocationTransferResponse>>>
{
    public async Task<Result<List<StockLocationTransferResponse>>> HandleAsync(GetAllStockLocationTransfersQuery query, CancellationToken cancellationToken = default)
    {
        var transfers = await dbContext.StockLocationTransfers
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .OrderByDescending(t => t.TransferDate)
            .ToListAsync(cancellationToken);

        return Result.Success(transfers.Select(StockLocationTransferMapper.ToResponse).ToList());
    }
}
