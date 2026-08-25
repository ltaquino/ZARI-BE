namespace ZARI.Application.Features.Inventory.GoodsReceipts.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED receipt has to go through RequestGoodsReceiptCancellation instead.
/// </summary>
public sealed class CancelGoodsReceiptCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelGoodsReceiptCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(CancelGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPTS", FormAction.Cancel, receipt.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptResponse>(Error.Forbidden("GoodsReceipt.Forbidden", "You do not have permission to cancel goods receipts for this branch."));

        if (receipt.Status == "CANCELLED")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.AlreadyCancelled", "This goods receipt is already cancelled."));

        if (receipt.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.RequiresCancellationRequest", "A posted goods receipt must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("GOODS_RECEIPT", receipt.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(cancelPendingResult.Error!);

        receipt.Status = "CANCELLED";
        receipt.CancelledBy = command.CancelledBy;
        receipt.CancelledAt = DateTimeOffset.UtcNow;
        receipt.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this goods receipt — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
