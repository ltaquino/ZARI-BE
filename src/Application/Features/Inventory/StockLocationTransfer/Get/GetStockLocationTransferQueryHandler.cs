namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Domain.Common;

public sealed class GetStockLocationTransferQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetStockLocationTransferQuery, Result<StockLocationTransferResponse>>
{
    public async Task<Result<StockLocationTransferResponse>> HandleAsync(GetStockLocationTransferQuery query, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);

        if (transfer is null)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{query.Id}' was not found."));

        return Result.Success(StockLocationTransferMapper.ToResponse(transfer));
    }
}
