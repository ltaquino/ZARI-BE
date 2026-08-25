namespace ZARI.Application.Features.Inventory.StockTransferRequests.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Domain.Common;

public sealed class GetStockTransferRequestQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetStockTransferRequestQuery, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(GetStockTransferRequestQuery query, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (request is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{query.Id}' was not found."));

        var canViewSource = await permissionService.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, request.SourceBranchId, cancellationToken);
        var canViewDest = await permissionService.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, request.DestBranchId, cancellationToken);
        if (!canViewSource && !canViewDest)
            return Result.Failure<StockTransferRequestResponse>(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to view this stock transfer request."));

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
