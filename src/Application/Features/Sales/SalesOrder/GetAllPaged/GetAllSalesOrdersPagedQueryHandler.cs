namespace ZARI.Application.Features.Sales.SalesOrders.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesOrdersPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesOrdersPagedQuery, Result<PagedResult<SalesOrderResponse>>>
{
    public async Task<Result<PagedResult<SalesOrderResponse>>> HandleAsync(GetAllSalesOrdersPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_ORDERS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<SalesOrderResponse>>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to view sales orders."));

        var baseQuery = dbContext.SalesOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.SoNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var orders = await baseQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<SalesOrderResponse>(orders.Select(SalesOrderMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
