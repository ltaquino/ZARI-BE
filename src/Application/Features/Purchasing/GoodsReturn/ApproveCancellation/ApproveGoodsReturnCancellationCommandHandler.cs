namespace ZARI.Application.Features.Purchasing.GoodsReturns.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors GoodsIssue's cancellation-approval (the same direction of stock movement —
/// an issue — as a Goods Return): reverse the stock ledger movements (restores the returned stock
/// via the shared ReverseStockMovementsCommand engine, which already handles both the receive and
/// issue directions symmetrically), reverse any serials issued, reverse the posted GL journal(s),
/// then decide the cancellation request.
/// </summary>
public sealed class ApproveGoodsReturnCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsReturnCancellationCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(ApproveGoodsReturnCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("GOODS_RETURNS", cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (goodsReturn.Status != "PENDING_CANCELLATION")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NotPendingCancellation", "Only a goods return pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RETURNS" && r.EntityId == goodsReturn.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this goods return."));

        var lineIds = goodsReturn.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("GoodsReturnLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(reverseStockResult.Error!);

        foreach (var line in goodsReturn.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var reverseSerialResult = await reverseIssueSerialHandler.HandleAsync(new ReverseIssueSerialCommand(line.ItemId, line.SerialNo), cancellationToken);
            if (!reverseSerialResult.IsSuccess)
                return Result.Failure<GoodsReturnResponse>(reverseSerialResult.Error!);
        }

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("GoodsReturn", goodsReturn.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {goodsReturn.ReturnNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(decideResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `goodsReturn`
        // this handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        goodsReturn.Status = "CANCELLED";
        goodsReturn.CancelledBy = command.ApproverUserId;
        goodsReturn.CancelledAt = cancelledAt;
        await dbContext.GoodsReturns.Where(r => r.Id == goodsReturn.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "CANCELLED")
                .SetProperty(r => r.CancelledBy, command.ApproverUserId)
                .SetProperty(r => r.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }
}
