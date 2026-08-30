namespace ZARI.Application.Features.Sales.DeliveryOrders.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Domain.Common;

public sealed class GetDeliveryOrderQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetDeliveryOrderQuery, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(GetDeliveryOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(d => d.Id == query.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.View, order.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to view deliveries for this branch."));

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }
}
