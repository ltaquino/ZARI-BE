namespace ZARI.Application.Features.Sales.SalesOrders.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Features.Sales.SalesOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetSalesOrderQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetSalesOrderQuery, Result<SalesOrderResponse>>
{
    public async Task<Result<SalesOrderResponse>> HandleAsync(GetSalesOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (order is null)
            return Result.Failure<SalesOrderResponse>(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.View, order.BranchId, cancellationToken))
            return Result.Failure<SalesOrderResponse>(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to view sales orders for this branch."));

        return Result.Success(SalesOrderMapper.ToResponse(order));
    }
}
