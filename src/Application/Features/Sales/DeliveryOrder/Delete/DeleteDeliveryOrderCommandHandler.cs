namespace ZARI.Application.Features.Sales.DeliveryOrders.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDeliveryOrderCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteDeliveryOrderCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders.FindAsync([command.Id], cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Delete, order.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to delete deliveries for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure(Error.Validation("DeliveryOrder.NotDraft", "Only draft deliveries can be deleted — cancel it instead."));

        dbContext.DeliveryOrders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
