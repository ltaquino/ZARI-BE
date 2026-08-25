namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePurchaseRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePurchaseRequestCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePurchaseRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PurchaseRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("PurchaseRequest.NotFound", $"Purchase request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Delete, request.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PurchaseRequest.Forbidden", "You do not have permission to delete this purchase request for this branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("PurchaseRequest.NotDraft", "Only draft purchase requests can be deleted — cancel it instead."));

        dbContext.PurchaseRequests.Remove(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
