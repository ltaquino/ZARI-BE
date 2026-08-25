namespace ZARI.Application.Features.Inventory.GoodsReceipts.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGoodsReceiptCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteGoodsReceiptCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts.FindAsync([command.Id], cancellationToken);
        if (receipt is null)
            return Result.Failure(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPTS", FormAction.Delete, receipt.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("GoodsReceipt.Forbidden", "You do not have permission to delete goods receipts for this branch."));

        if (receipt.Status != "DRAFT")
            return Result.Failure(Error.Validation("GoodsReceipt.NotDraft", "Only draft goods receipts can be deleted — cancel it instead."));

        dbContext.GoodsReceipts.Remove(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
