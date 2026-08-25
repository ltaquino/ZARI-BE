namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted.</summary>
public sealed class RejectGoodsReceiptPoCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectGoodsReceiptPoCancellationCommand, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(RejectGoodsReceiptPoCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("GOODS_RECEIPT_PO", cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (receipt.Status != "PENDING_CANCELLATION")
            return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.NotPendingCancellation", "Only a goods receipt pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RECEIPT_PO" && r.EntityId == receipt.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this goods receipt."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(decideResult.Error!);

        receipt.Status = "POSTED";
        receipt.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }
}
