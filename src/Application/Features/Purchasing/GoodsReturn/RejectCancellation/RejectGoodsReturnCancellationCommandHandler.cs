namespace ZARI.Application.Features.Purchasing.GoodsReturns.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted.</summary>
public sealed class RejectGoodsReturnCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectGoodsReturnCancellationCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(RejectGoodsReturnCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("GOODS_RETURNS", cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (goodsReturn.Status != "PENDING_CANCELLATION")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NotPendingCancellation", "Only a goods return pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RETURNS" && r.EntityId == goodsReturn.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this goods return."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(decideResult.Error!);

        goodsReturn.Status = "POSTED";
        goodsReturn.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }
}
