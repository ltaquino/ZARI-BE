namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePurchaseReturnReasonCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePurchaseReturnReasonCommand>
{
    public async Task<Result> HandleAsync(DeletePurchaseReturnReasonCommand command, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.PurchaseReturnReasons.FindAsync([command.Id], cancellationToken);
        if (reason is null)
            return Result.Failure(Error.NotFound("PurchaseReturnReason.NotFound", $"Purchase return reason with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("PurchaseReturnReason.Forbidden", "You do not have permission to delete purchase return reasons."));

        dbContext.PurchaseReturnReasons.Remove(reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
