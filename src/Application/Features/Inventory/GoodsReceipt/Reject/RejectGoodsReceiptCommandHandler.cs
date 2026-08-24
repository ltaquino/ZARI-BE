namespace ZARI.Application.Features.Inventory.GoodsReceipts.Reject;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_APPROVAL -> DRAFT, so the encoder can fix the issue the checker flagged and resubmit.</summary>
public sealed class RejectGoodsReceiptCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<RejectGoodsReceiptCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(RejectGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (receipt.Status != "PENDING_APPROVAL")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotPendingApproval", "Only goods receipts pending approval can be rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RECEIPT" && r.EntityId == receipt.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this goods receipt."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(decideResult.Error!);

        receipt.Status = "DRAFT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "REJECTED", "ACTIVITY",
                $"rejected this goods receipt — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
