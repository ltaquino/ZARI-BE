namespace ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockTransferRequestsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockTransferRequestsQuery, Result<List<StockTransferRequestResponse>>>
{
    public async Task<Result<List<StockTransferRequestResponse>>> HandleAsync(GetAllStockTransferRequestsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockTransferRequestResponse>>(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to view stock transfer requests."));

        var requests = await dbContext.StockTransferRequests.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(cancellationToken);

        return Result.Success(requests.Select(StockTransferRequestMapper.ToResponse).ToList());
    }
}
