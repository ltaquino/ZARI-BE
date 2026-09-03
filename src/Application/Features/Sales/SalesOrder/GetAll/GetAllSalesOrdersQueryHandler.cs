namespace ZARI.Application.Features.Sales.SalesOrders.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesOrdersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesOrdersQuery, Result<List<SalesOrderResponse>>>
{
    public async Task<Result<List<SalesOrderResponse>>> HandleAsync(GetAllSalesOrdersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_ORDERS", FormAction.View, cancellationToken))
            return Result.Failure<List<SalesOrderResponse>>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to view sales orders."));

        var orders = await dbContext.SalesOrders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return Result.Success(orders.Select(SalesOrderMapper.ToResponse).ToList());
    }
}
