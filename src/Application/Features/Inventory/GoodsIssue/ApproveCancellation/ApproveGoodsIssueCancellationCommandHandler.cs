namespace ZARI.Application.Features.Inventory.GoodsIssues.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors the FE prototype's approveGoodsIssueCancellation: reverse the stock ledger
/// movements, reverse any serials issued, reverse the posted GL journal(s), then decide the
/// cancellation request.
/// </summary>
public sealed class ApproveGoodsIssueCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsIssueCancellationCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(ApproveGoodsIssueCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("GOODS_ISSUES", cancellationToken))
            return Result.Failure<GoodsIssueResponse>(Error.Forbidden("GoodsIssue.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (issue.Status != "PENDING_CANCELLATION")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotPendingCancellation", "Only a goods issue pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_ISSUE" && r.EntityId == issue.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this goods issue."));

        // Decide before any reversal side-effect — see ApproveGoodsReceiptCancellationCommandHandler's
        // doc comment for why (a failed decide must leave nothing reversed yet, so it stays retryable).
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(decideResult.Error!);

        var lineIds = issue.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("GoodsIssueLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(reverseStockResult.Error!);

        foreach (var line in issue.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var reverseSerialResult = await reverseIssueSerialHandler.HandleAsync(new ReverseIssueSerialCommand(line.ItemId, line.SerialNo), cancellationToken);
            if (!reverseSerialResult.IsSuccess)
                return Result.Failure<GoodsIssueResponse>(reverseSerialResult.Error!);
        }

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("GoodsIssue", issue.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {issue.GiNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(reverseJournalsResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `issue` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        issue.Status = "CANCELLED";
        issue.CancelledBy = command.ApproverUserId;
        issue.CancelledAt = cancelledAt;
        await dbContext.GoodsIssues.Where(i => i.Id == issue.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, "CANCELLED")
                .SetProperty(i => i.CancelledBy, command.ApproverUserId)
                .SetProperty(i => i.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
