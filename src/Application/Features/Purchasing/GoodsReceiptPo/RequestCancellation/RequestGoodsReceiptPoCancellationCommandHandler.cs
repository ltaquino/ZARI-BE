namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestGoodsReceiptPoCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestGoodsReceiptPoCancellationCommand, Result<GoodsReceiptPoResponse>>
{
    public async Task<Result<GoodsReceiptPoResponse>> HandleAsync(RequestGoodsReceiptPoCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.GoodsReceiptPos
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (receipt is null)
            return Result.Failure<GoodsReceiptPoResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Cancel, receipt.BranchId, cancellationToken))
            return Result.Failure<GoodsReceiptPoResponse>(Error.Forbidden("GoodsReceiptPo.Forbidden", "You do not have permission to request cancellation of goods receipts (PO) for this branch."));

        if (receipt.Status != "POSTED")
            return Result.Failure<GoodsReceiptPoResponse>(Error.Validation("GoodsReceiptPo.NotPosted", "Only a posted goods receipt can have its cancellation requested."));

        var downstreamCheckResult = await CheckNoDownstreamPostedDocumentsAsync(dbContext, receipt.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(downstreamCheckResult.Error!);

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(submitResult.Error!);

        receipt.Status = "PENDING_CANCELLATION";
        receipt.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RECEIPT_PO", receipt.Id.ToString(), receipt.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReceiptPoResponse>(notifyResult.Error!);

        return Result.Success(GoodsReceiptPoMapper.ToResponse(receipt));
    }

    /// <summary>
    /// Blocks cancelling a GRPO that's already been (partially or fully) billed or returned —
    /// reversing the original receive/GRNI journal out from under a POSTED AP Invoice or Goods
    /// Return would strand a stray, never-clearing GRNI balance and let a vendor get paid for
    /// goods the ledger now says were never received. Checked here (friendly, at request time)
    /// and again in ApproveGoodsReceiptPoCancellationCommandHandler (authoritative, since an
    /// invoice/return could be approved in the gap between request and approval).
    /// </summary>
    internal static async Task<Result> CheckNoDownstreamPostedDocumentsAsync(IAppDbContext dbContext, List<Guid> lineIds, CancellationToken cancellationToken)
    {
        var hasPostedInvoice = await dbContext.ApInvoiceLines
            .AnyAsync(l => l.GoodsReceiptPoLineId.HasValue && lineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.ApInvoice.Status == "POSTED", cancellationToken);
        if (hasPostedInvoice)
            return Result.Failure(Error.Validation("GoodsReceiptPo.HasPostedApInvoice", "This goods receipt can't be cancelled — a posted AP Invoice already bills against it."));

        var hasPostedReturn = await dbContext.GoodsReturnLines
            .AnyAsync(l => l.GoodsReceiptPoLineId.HasValue && lineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.GoodsReturn.Status == "POSTED", cancellationToken);
        if (hasPostedReturn)
            return Result.Failure(Error.Validation("GoodsReceiptPo.HasPostedGoodsReturn", "This goods receipt can't be cancelled — a posted Goods Return already references it."));

        return Result.Success();
    }
}
