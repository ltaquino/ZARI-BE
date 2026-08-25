namespace ZARI.Application.Features.Inventory.GoodsReceipts.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors the FE prototype's approveGoodsReceiptCancellation: reverse the stock ledger
/// movements, reverse any serials received, reverse the posted GL journal(s), then decide the
/// cancellation request. Note: like the FE prototype, this does NOT reverse StockLocationBalance
/// (receiveIntoLocation) — a pre-existing gap being faithfully replicated, not introduced here.
/// </summary>
public sealed class ApproveGoodsReceiptCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseReceiveSerialCommand, Result> reverseReceiveSerialHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsReceiptCancellationCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(ApproveGoodsReceiptCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("GOODS_RECEIPTS", cancellationToken))
            return Result.Failure<GoodsReceiptResponse>(Error.Forbidden("GoodsReceipt.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (receipt.Status != "PENDING_CANCELLATION")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotPendingCancellation", "Only a goods receipt pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RECEIPT" && r.EntityId == receipt.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this goods receipt."));

        var lineIds = receipt.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("GoodsReceiptLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(reverseStockResult.Error!);

        var revertTo = receipt.ReceiptType == "TRANSFER_IN" ? "IN_TRANSIT" : "REMOVE";
        foreach (var line in receipt.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var reverseSerialResult = await reverseReceiveSerialHandler.HandleAsync(new ReverseReceiveSerialCommand(line.ItemId, line.SerialNo, revertTo), cancellationToken);
            if (!reverseSerialResult.IsSuccess)
                return Result.Failure<GoodsReceiptResponse>(reverseSerialResult.Error!);
        }

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("GoodsReceipt", receipt.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {receipt.GrNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(decideResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `receipt` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        receipt.Status = "CANCELLED";
        receipt.CancelledBy = command.ApproverUserId;
        receipt.CancelledAt = cancelledAt;
        await dbContext.GoodsReceipts.Where(r => r.Id == receipt.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "CANCELLED")
                .SetProperty(r => r.CancelledBy, command.ApproverUserId)
                .SetProperty(r => r.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
