namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkInTransit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;

/// <summary>PREPARING -> IN_TRANSIT. The source branch marks it shipped; only meaningful once posted.</summary>
public sealed class MarkGoodsIssueInTransitCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<MarkGoodsIssueInTransitCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(MarkGoodsIssueInTransitCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Edit, issue.BranchId, cancellationToken))
            return Result.Failure<GoodsIssueResponse>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to mark this shipment as in transit for this branch."));

        if (issue.Status != "POSTED")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotPosted", "Only a posted goods issue has a shipment to track."));

        if (issue.ShipmentStatus != "PREPARING")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotPreparing", "This shipment is not in Preparing status."));

        issue.ShipmentStatus = "IN_TRANSIT";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "SHIPMENT_IN_TRANSIT", "ACTIVITY",
                "marked this shipment as in transit", command.UserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
