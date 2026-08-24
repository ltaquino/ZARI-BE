namespace ZARI.Application.Features.Inventory.GoodsReceipts.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Application.Features.Inventory.GoodsReceipts.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestGoodsReceiptCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<RequestGoodsReceiptCancellationCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(RequestGoodsReceiptCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (receipt.Status != "POSTED")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotPosted", "Only a posted goods receipt can have its cancellation requested."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(submitResult.Error!);

        receipt.Status = "PENDING_CANCELLATION";
        receipt.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
