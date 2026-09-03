namespace ZARI.Application.Features.Inventory.StockLocationTransfers.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockLocationTransfersPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockLocationTransfersPagedQuery, Result<PagedResult<StockLocationTransferResponse>>>
{
    public async Task<Result<PagedResult<StockLocationTransferResponse>>> HandleAsync(GetAllStockLocationTransfersPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_LOCATION_TRANSFERS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<StockLocationTransferResponse>>(Error.Forbidden("StockLocationTransfer.Forbidden", "You do not have permission to view stock location transfers."));

        var baseQuery = dbContext.StockLocationTransfers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.TransferNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var transfers = await baseQuery
            .OrderByDescending(t => t.TransferDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockLocationTransferResponse>(transfers.Select(StockLocationTransferMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
