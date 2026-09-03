namespace ZARI.Application.Features.Inventory.StockTransferRequests.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockTransferRequestsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockTransferRequestsPagedQuery, Result<PagedResult<StockTransferRequestResponse>>>
{
    public async Task<Result<PagedResult<StockTransferRequestResponse>>> HandleAsync(GetAllStockTransferRequestsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<StockTransferRequestResponse>>(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to view stock transfer requests."));

        var baseQuery = dbContext.StockTransferRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.RequestNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var requests = await baseQuery
            .OrderByDescending(r => r.RequestDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockTransferRequestResponse>(requests.Select(StockTransferRequestMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
