namespace ZARI.Application.Features.Inventory.StockAdjustments.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED adjustment has to go through RequestStockAdjustmentCancellation instead.
/// </summary>
public sealed class CancelStockAdjustmentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<CancelStockAdjustmentCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(CancelStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status == "CANCELLED")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.AlreadyCancelled", "This stock adjustment is already cancelled."));

        if (adjustment.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.RequiresCancellationRequest", "A posted stock adjustment must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(cancelPendingResult.Error!);

        adjustment.Status = "CANCELLED";
        adjustment.CancelledBy = command.CancelledBy;
        adjustment.CancelledAt = DateTimeOffset.UtcNow;
        adjustment.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this stock adjustment — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
