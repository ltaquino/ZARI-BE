namespace ZARI.Application.Features.Inventory.StockAdjustments.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestStockAdjustmentCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<RequestStockAdjustmentCancellationCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(RequestStockAdjustmentCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status != "POSTED")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.NotPosted", "Only a posted stock adjustment can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(submitResult.Error!);

        adjustment.Status = "PENDING_CANCELLATION";
        adjustment.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }
}
