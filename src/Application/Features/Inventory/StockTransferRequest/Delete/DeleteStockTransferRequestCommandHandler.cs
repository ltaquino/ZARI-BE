namespace ZARI.Application.Features.Inventory.StockTransferRequests.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStockTransferRequestCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteStockTransferRequestCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests.FindAsync([command.Id], cancellationToken);
        if (request is null)
            return Result.Failure(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Delete, request.DestBranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("StockTransferRequest.Forbidden", "You do not have permission to delete this stock transfer request for the requesting branch."));

        if (request.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockTransferRequest.NotDraft", "Only draft stock transfer requests can be deleted — cancel it instead."));

        dbContext.StockTransferRequests.Remove(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
