namespace ZARI.Application.Features.Inventory.StockTransferRequests.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// DRAFT / PENDING_APPROVAL / APPROVED -> CANCELLED. The requesting branch withdraws its own
/// request. No posted-document two-tier flow needed — this table has zero stock/GL impact.
/// </summary>
public sealed class CancelStockTransferRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<CancelStockTransferRequestCommand, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(CancelStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{command.Id}' was not found."));

        if (request.Status is "CANCELLED" or "DECLINED")
            return Result.Failure<StockTransferRequestResponse>(Error.Validation("StockTransferRequest.CannotCancel", "This stock transfer request can no longer be cancelled."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(cancelPendingResult.Error!);

        request.Status = "CANCELLED";
        request.CancelledBy = command.CancelledBy;
        request.CancelledAt = DateTimeOffset.UtcNow;
        request.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString(), request.DestBranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this stock transfer request — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(notifyResult.Error!);

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
