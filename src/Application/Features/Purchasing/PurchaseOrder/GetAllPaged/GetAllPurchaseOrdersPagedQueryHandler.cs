namespace ZARI.Application.Features.Purchasing.PurchaseOrders.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllPurchaseOrdersPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPurchaseOrdersPagedQuery, Result<PagedResult<PurchaseOrderResponse>>>
{
    public async Task<Result<PagedResult<PurchaseOrderResponse>>> HandleAsync(GetAllPurchaseOrdersPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PURCHASE_ORDERS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<PurchaseOrderResponse>>(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to view purchase orders."));

        var baseQuery = dbContext.PurchaseOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.PoNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var orders = await baseQuery
            .OrderByDescending(p => p.OrderDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<PurchaseOrderResponse>(orders.Select(PurchaseOrderMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
