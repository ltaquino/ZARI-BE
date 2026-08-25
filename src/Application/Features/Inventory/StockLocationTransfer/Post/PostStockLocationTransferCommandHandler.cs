namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Post;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Inventory.StockLocationBalances.Move;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// DRAFT -> POSTED. No approval step — a branch manager posts directly, mirroring the FE
/// prototype's postStockLocationTransfer. Moves each line's qty between bins via the existing
/// MoveBetweenLocationsCommand; no GL journal (see StockLocationTransfer's doc comment).
/// </summary>
public sealed class PostStockLocationTransferCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<MoveBetweenLocationsCommand, Result> moveBetweenLocationsHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<PostStockLocationTransferCommand, Result<StockLocationTransferResponse>>
{
    public async Task<Result<StockLocationTransferResponse>> HandleAsync(PostStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (transfer is null)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Approve, transfer.BranchId, cancellationToken))
            return Result.Failure<StockLocationTransferResponse>(Error.Forbidden("StockLocationTransfer.Forbidden", "You do not have permission to post bin transfers for this branch."));

        if (transfer.Status != "DRAFT")
            return Result.Failure<StockLocationTransferResponse>(Error.Validation("StockLocationTransfer.NotDraft", "Only a draft bin transfer can be posted."));

        if (transfer.Lines.Count == 0)
            return Result.Failure<StockLocationTransferResponse>(Error.Validation("StockLocationTransfer.NoLines", "Add at least one line before posting."));

        foreach (var line in transfer.Lines)
        {
            var moveResult = await moveBetweenLocationsHandler.HandleAsync(
                new MoveBetweenLocationsCommand(line.ItemId, transfer.WarehouseId, line.BatchNo, line.FromLocationId, line.ToLocationId, line.Qty),
                cancellationToken);
            if (!moveResult.IsSuccess)
                return Result.Failure<StockLocationTransferResponse>(moveResult.Error!);
        }

        // MoveBetweenLocationsCommandHandler runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `transfer` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        transfer.Status = "POSTED";
        transfer.PostedBy = command.PostedBy;
        await dbContext.StockLocationTransfers.Where(t => t.Id == transfer.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, "POSTED")
                .SetProperty(t => t.PostedBy, command.PostedBy), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_LOCATION_TRANSFER", transfer.Id.ToString(), transfer.BranchId, "APPROVED", "ACTIVITY",
                "posted this bin transfer", command.PostedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockLocationTransferResponse>(notifyResult.Error!);

        return Result.Success(StockLocationTransferMapper.ToResponse(transfer));
    }
}
