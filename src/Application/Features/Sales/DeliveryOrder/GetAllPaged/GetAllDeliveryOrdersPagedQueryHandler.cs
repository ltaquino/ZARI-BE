namespace ZARI.Application.Features.Sales.DeliveryOrders.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllDeliveryOrdersPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllDeliveryOrdersPagedQuery, Result<PagedResult<DeliveryOrderResponse>>>
{
    public async Task<Result<PagedResult<DeliveryOrderResponse>>> HandleAsync(GetAllDeliveryOrdersPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DELIVERIES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<DeliveryOrderResponse>>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to view delivery orders."));

        var baseQuery = dbContext.DeliveryOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.DoNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var orders = await baseQuery
            .OrderByDescending(d => d.DeliveryDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<DeliveryOrderResponse>(orders.Select(DeliveryOrderMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
