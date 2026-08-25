namespace ZARI.Application.Features.Purchasing.GoodsReturns.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGoodsReturnCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteGoodsReturnCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns.FindAsync([command.Id], cancellationToken);
        if (goodsReturn is null)
            return Result.Failure(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Delete, goodsReturn.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to delete goods returns for this branch."));

        if (goodsReturn.Status != "DRAFT")
            return Result.Failure(Error.Validation("GoodsReturn.NotDraft", "Only draft goods returns can be deleted — cancel it instead."));

        dbContext.GoodsReturns.Remove(goodsReturn);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
