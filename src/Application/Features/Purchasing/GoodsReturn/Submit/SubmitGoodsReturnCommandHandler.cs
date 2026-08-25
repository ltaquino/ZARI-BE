namespace ZARI.Application.Features.Purchasing.GoodsReturns.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitGoodsReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitGoodsReturnCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(SubmitGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Edit, goodsReturn.BranchId, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to submit goods returns for this branch."));

        if (goodsReturn.Status != "DRAFT")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NotDraft", "Only draft goods returns can be submitted for approval."));

        if (goodsReturn.Lines.Count == 0)
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(submitResult.Error!);

        goodsReturn.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this goods return for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }
}
