namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkDelivered;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// IN_TRANSIT -> DELIVERED. The destination branch confirms physical arrival — they're the ones
/// who see the truck show up, before anyone there has necessarily encoded the GR yet.
/// </summary>
public sealed class MarkGoodsIssueDeliveredCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<MarkGoodsIssueDeliveredCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(MarkGoodsIssueDeliveredCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (issue.Status != "POSTED")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotPosted", "Only a posted goods issue has a shipment to track."));

        if (issue.ShipmentStatus != "IN_TRANSIT")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotInTransit", "This shipment must be in transit before it can be marked delivered."));

        issue.ShipmentStatus = "DELIVERED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "SHIPMENT_DELIVERED", "ACTIVITY",
                "marked this shipment as delivered", command.UserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
