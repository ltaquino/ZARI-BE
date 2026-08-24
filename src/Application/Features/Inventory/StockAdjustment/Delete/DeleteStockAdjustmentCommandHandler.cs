namespace ZARI.Application.Features.Inventory.StockAdjustments.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStockAdjustmentCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteStockAdjustmentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.FindAsync([command.Id], cancellationToken);
        if (adjustment is null)
            return Result.Failure(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockAdjustment.NotDraft", "Only draft stock adjustments can be deleted — cancel it instead."));

        dbContext.StockAdjustments.Remove(adjustment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
