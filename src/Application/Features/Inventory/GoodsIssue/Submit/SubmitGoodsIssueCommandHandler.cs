namespace ZARI.Application.Features.Inventory.GoodsIssues.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitGoodsIssueCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<SubmitGoodsIssueCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(SubmitGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (issue.Status != "DRAFT")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotDraft", "Only draft goods issues can be submitted for approval."));

        if (issue.Lines.Count == 0)
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(submitResult.Error!);

        issue.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this goods issue for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
