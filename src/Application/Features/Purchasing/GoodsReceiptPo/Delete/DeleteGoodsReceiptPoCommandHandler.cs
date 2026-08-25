namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGoodsReceiptPoCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteGoodsReceiptPoCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteGoodsReceiptPoCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos.FindAsync([command.Id], cancellationToken);
        if (receipt is null)
            return Result.Failure(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Delete, receipt.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to delete goods receipts (PO) for this branch."));

        if (receipt.Status != "DRAFT")
            return Result.Failure(Error.Validation("GoodsReceiptPo.NotDraft", "Only draft goods receipts can be deleted — cancel it instead."));

        dbContext.GoodsReceiptPos.Remove(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
