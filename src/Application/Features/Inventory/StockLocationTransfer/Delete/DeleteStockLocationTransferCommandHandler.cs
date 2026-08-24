namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStockLocationTransferCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteStockLocationTransferCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers.FindAsync([command.Id], cancellationToken);
        if (transfer is null)
            return Result.Failure(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{command.Id}' was not found."));

        if (transfer.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockLocationTransfer.NotDraft", "Only a draft bin transfer can be deleted — cancel it instead."));

        dbContext.StockLocationTransfers.Remove(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
