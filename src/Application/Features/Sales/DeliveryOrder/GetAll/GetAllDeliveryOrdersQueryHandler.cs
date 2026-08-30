namespace ZARI.Application.Features.Sales.DeliveryOrders.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetAllDeliveryOrdersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllDeliveryOrdersQuery, Result<List<DeliveryOrderResponse>>>
{
    public async Task<Result<List<DeliveryOrderResponse>>> HandleAsync(GetAllDeliveryOrdersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DELIVERIES", FormAction.View, cancellationToken))
            return Result.Failure<List<DeliveryOrderResponse>>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to view deliveries."));

        var orders = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(d => d.DeliveryDate)
            .ToListAsync(cancellationToken);

        return Result.Success(orders.Select(DeliveryOrderMapper.ToResponse).ToList());
    }
}
