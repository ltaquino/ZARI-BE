namespace ZARI.Application.Features.Purchasing.GoodsReturns.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED return has to go through RequestGoodsReturnCancellation instead.
/// </summary>
public sealed class CancelGoodsReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelGoodsReturnCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(CancelGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Cancel, goodsReturn.BranchId, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to cancel goods returns for this branch."));

        if (goodsReturn.Status == "CANCELLED")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.AlreadyCancelled", "This goods return is already cancelled."));

        if (goodsReturn.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.RequiresCancellationRequest", "A posted goods return must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("GOODS_RETURNS", goodsReturn.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(cancelPendingResult.Error!);

        goodsReturn.Status = "CANCELLED";
        goodsReturn.CancelledBy = command.CancelledBy;
        goodsReturn.CancelledAt = DateTimeOffset.UtcNow;
        goodsReturn.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this goods return — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }
}
