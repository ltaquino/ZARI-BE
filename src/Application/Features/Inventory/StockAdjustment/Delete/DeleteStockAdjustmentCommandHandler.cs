namespace ZARI.Application.Features.Inventory.StockAdjustments.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;

public sealed class DeleteStockAdjustmentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteStockAdjustmentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.FindAsync([command.Id], cancellationToken);
        if (adjustment is null)
            return Result.Failure(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Delete, adjustment.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("StockAdjustment.Forbidden", "You do not have permission to delete stock adjustments for this branch."));

        if (adjustment.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockAdjustment.NotDraft", "Only draft stock adjustments can be deleted — cancel it instead."));

        dbContext.StockAdjustments.Remove(adjustment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
