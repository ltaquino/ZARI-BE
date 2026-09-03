namespace ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseOrdersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPurchaseOrdersQuery, Result<List<PurchaseOrderResponse>>>
{
    public async Task<Result<List<PurchaseOrderResponse>>> HandleAsync(GetAllPurchaseOrdersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_ORDERS", FormAction.View, cancellationToken))
            return Result.Failure<List<PurchaseOrderResponse>>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to view purchase orders."));

        var orders = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync(cancellationToken);

        return Result.Success(orders.Select(PurchaseOrderMapper.ToResponse).ToList());
    }
}
