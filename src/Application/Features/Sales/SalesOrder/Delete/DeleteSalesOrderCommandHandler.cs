namespace ZARI.Application.Features.Sales.SalesOrders.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteSalesOrderCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteSalesOrderCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteSalesOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders.FindAsync([command.Id], cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("SalesOrder.NotFound", $"Sales order with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_ORDERS", FormAction.Delete, order.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("SalesOrder.Forbidden", "You do not have permission to delete sales orders for this branch."));

        if (order.Status != "DRAFT")
            return Result.Failure(Error.Validation("SalesOrder.NotDraft", "Only draft sales orders can be deleted — cancel it instead."));

        dbContext.SalesOrders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
