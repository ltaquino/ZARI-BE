namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// DRAFT -> CANCELLED only. Nothing is ever posted through an approval workflow for this
/// document (see StockLocationTransfer's doc comment), so there is no pending approval request to
/// clean up and no posted-state reversal to perform — direct cancel is the only cancel path.
/// </summary>
public sealed class CancelStockLocationTransferCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelStockLocationTransferCommand, Result<StockLocationTransferResponse>>
{
    public async Task<Result<StockLocationTransferResponse>> HandleAsync(CancelStockLocationTransferCommand command, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockLocationTransfers
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.Lines).ThenInclude(l => l.FromLocation)
            .Include(t => t.Lines).ThenInclude(l => l.ToLocation)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (transfer is null)
            return Result.Failure<StockLocationTransferResponse>(Error.NotFound("StockLocationTransfer.NotFound", $"Bin transfer with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Cancel, transfer.BranchId, cancellationToken))
            return Result.Failure<StockLocationTransferResponse>(Error.Forbidden("StockLocationTransfer.Forbidden", "You do not have permission to cancel bin transfers for this branch."));

        if (transfer.Status != "DRAFT")
            return Result.Failure<StockLocationTransferResponse>(Error.Validation("StockLocationTransfer.NotDraft", "Only a draft bin transfer can be cancelled directly."));

        transfer.Status = "CANCELLED";
        transfer.CancelledBy = command.CancelledBy;
        transfer.CancelledAt = DateTimeOffset.UtcNow;
        transfer.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_LOCATION_TRANSFER", transfer.Id.ToString(), transfer.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this bin transfer — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockLocationTransferResponse>(notifyResult.Error!);

        return Result.Success(StockLocationTransferMapper.ToResponse(transfer));
    }
}
