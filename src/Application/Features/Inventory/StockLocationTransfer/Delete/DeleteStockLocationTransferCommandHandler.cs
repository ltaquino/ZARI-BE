namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStockLocationTransferCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteStockLocationTransferCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers.FindAsync([command.Id], cancellationToken);
        if (transfer is null)
            return Result.Failure(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Delete, transfer.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("StockLocationTransfer.Forbidden", "You do not have permission to delete bin transfers for this branch."));

        if (transfer.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockLocationTransfer.NotDraft", "Only a draft bin transfer can be deleted — cancel it instead."));

        dbContext.StockLocationTransfers.Remove(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
