namespace ZARI.Application.Features.Inventory.StockAdjustments.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted.</summary>
public sealed class RejectStockAdjustmentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<RejectStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(RejectStockAdjustmentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status != "PENDING_CANCELLATION")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.NotPendingCancellation", "Only a stock adjustment pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "STOCK_ADJUSTMENT" && r.EntityId == adjustment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this stock adjustment."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(decideResult.Error!);

        adjustment.Status = "POSTED";
        adjustment.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
