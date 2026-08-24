namespace ZARI.Application.Features.Inventory.StockAdjustments.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors the FE prototype's approveStockAdjustmentCancellation: reverse the stock
/// ledger movements (one batch call handles both the receive-like and issue-like lines), reverse
/// any serials moved, reverse the posted GL journal(s), then decide the cancellation request.
/// </summary>
public sealed class ApproveStockAdjustmentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseReceiveSerialCommand, Result> reverseReceiveSerialHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<ApproveStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(ApproveStockAdjustmentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status != "PENDING_CANCELLATION")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.NotPendingCancellation", "Only a stock adjustment pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "STOCK_ADJUSTMENT" && r.EntityId == adjustment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this stock adjustment."));

        var lineIds = adjustment.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("StockAdjustmentLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(reverseStockResult.Error!);

        foreach (var line in adjustment.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            if (line.VarianceQty > 0.0001m)
            {
                var reverseResult = await reverseReceiveSerialHandler.HandleAsync(new ReverseReceiveSerialCommand(line.ItemId, line.SerialNo, "REMOVE"), cancellationToken);
                if (!reverseResult.IsSuccess)
                    return Result.Failure<StockAdjustmentResponse>(reverseResult.Error!);
            }
            else if (line.VarianceQty < -0.0001m)
            {
                var reverseResult = await reverseIssueSerialHandler.HandleAsync(new ReverseIssueSerialCommand(line.ItemId, line.SerialNo), cancellationToken);
                if (!reverseResult.IsSuccess)
                    return Result.Failure<StockAdjustmentResponse>(reverseResult.Error!);
            }
        }

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("StockAdjustment", adjustment.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {adjustment.AdjustmentNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(decideResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `adjustment`
        // this handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        adjustment.Status = "CANCELLED";
        adjustment.CancelledBy = command.ApproverUserId;
        adjustment.CancelledAt = cancelledAt;
        await dbContext.StockAdjustments.Where(a => a.Id == adjustment.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, "CANCELLED")
                .SetProperty(a => a.CancelledBy, command.ApproverUserId)
                .SetProperty(a => a.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
