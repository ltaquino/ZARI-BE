namespace ZARI.Application.Features.Inventory.GoodsReceipts.Submit;

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

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitGoodsReceiptCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<SubmitGoodsReceiptCommand, Result<GoodsReceiptResponse>>
{
    public async Task<Result<GoodsReceiptResponse>> HandleAsync(SubmitGoodsReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceipts
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptResponse>(Error.NotFound("GoodsReceipt.NotFound", $"Goods receipt with ID '{command.Id}' was not found."));

        if (receipt.Status != "DRAFT")
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NotDraft", "Only draft goods receipts can be submitted for approval."));

        if (receipt.Lines.Count == 0)
            return Result.Failure<GoodsReceiptResponse>(Error.Validation("GoodsReceipt.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(submitResult.Error!);

        receipt.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT", receipt.Id.ToString(), receipt.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this goods receipt for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptMapper.ToResponse(receipt));
    }
}
