namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePurchaseOrderCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePurchaseOrderCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders.FindAsync([command.Id], cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("PurchaseOrder.NotFound", $"Purchase order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Delete, order.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PurchaseOrder.Forbidden", "You do not have permission to delete purchase orders for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure(Error.Validation("PurchaseOrder.NotDraft", "Only draft purchase orders can be deleted — cancel it instead."));

        dbContext.PurchaseOrders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
