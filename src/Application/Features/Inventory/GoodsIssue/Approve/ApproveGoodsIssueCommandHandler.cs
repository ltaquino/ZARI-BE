namespace ZARI.Application.Features.Inventory.GoodsIssues.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Approving IS the "checked by" record (see ApprovalAction). Mirrors
/// the FE prototype's approveGoodsIssue orchestration (data/inventory/goodsIssues.ts): issue every
/// line from the stock ledger as one batch, decide the approval request, issue serials, then post
/// the balancing GL journal. Reuses every already-migrated engine via injected handlers rather than
/// duplicating their logic — same composition pattern as ApproveGoodsReceiptCommandHandler.
/// </summary>
public sealed class ApproveGoodsIssueCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsIssueCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(ApproveGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (issue is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Approve, issue.BranchId, cancellationToken))
            return Result.Failure<GoodsIssueResponse>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to approve goods issues for this branch."));

        if (issue.Status != "PENDING_APPROVAL")
            return Result.Failure<GoodsIssueResponse>(Error.Validation("GoodsIssue.NotPendingApproval", "Only goods issues pending approval can be approved."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_ISSUE" && r.EntityId == issue.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this goods issue."));

        var issueLines = issue.Lines.Select(line => new IssueStockLineItem(
            line.ItemId, issue.BranchId, issue.WarehouseId, line.BatchNo, line.QtyIssued,
            "GoodsIssueLine", line.Id.ToString(), issue.GiDate, null)).ToList();

        var issueStockResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(issueLines), cancellationToken);
        if (!issueStockResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(issueStockResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(decideResult.Error!);

        var costsByReferenceId = issueStockResult.Value!.CostsByReferenceId;
        foreach (var line in issue.Lines)
        {
            if (costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost))
                line.UnitCost = cost;
        }

        var isTransfer = issue.ReferenceType == "STOCK_TRANSFER";
        var serialDisposition = isTransfer ? "IN_TRANSIT" : "DISPOSED";
        foreach (var line in issue.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await issueSerialHandler.HandleAsync(new IssueSerialCommand(line.ItemId, line.SerialNo, serialDisposition), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<GoodsIssueResponse>(serialResult.Error!);
        }

        var journalResult = await PostInventoryJournalAsync(issue, isTransfer, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(journalResult.Error!);

        // IssueStockLinesCommand runs its own retryable transaction and calls ChangeTracker.Clear()
        // at the start of every attempt — that detaches the `issue`/lines this handler loaded
        // earlier, so mutating them and calling SaveChangesAsync would silently persist nothing.
        // ExecuteUpdateAsync writes directly, independent of whatever the tracker currently holds.
        foreach (var line in issue.Lines)
        {
            if (!costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost)) continue;
            var lineId = line.Id;
            await dbContext.GoodsIssueLines.Where(l => l.Id == lineId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UnitCost, cost), cancellationToken);
        }

        issue.Status = "POSTED";
        issue.ShipmentStatus = isTransfer ? "PREPARING" : null;
        await dbContext.GoodsIssues.Where(i => i.Id == issue.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, "POSTED")
                .SetProperty(i => i.ShipmentStatus, issue.ShipmentStatus), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "APPROVED", "ACTIVITY",
                "approved this goods issue", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }

    private async Task<Result> PostInventoryJournalAsync(GoodsIssue issue, bool isTransfer, CancellationToken cancellationToken)
    {
        var creditsByAccount = new Dictionary<Guid, decimal>();
        foreach (var line in issue.Lines)
        {
            var accountResult = Guid.TryParse(line.Item.InventoryAccountId, out var explicitAccountId)
                ? Result.Success(explicitAccountId)
                : await GetDefaultAccountIdAsync("1400", "Inventory Asset", cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.Error!);

            var amount = Math.Round(line.QtyIssued * line.UnitCost, 4);
            creditsByAccount[accountResult.Value] = creditsByAccount.GetValueOrDefault(accountResult.Value) + amount;
        }

        var totalValue = creditsByAccount.Values.Sum();
        if (totalValue <= 0)
            return Result.Success();

        var debitAccountResult = isTransfer
            ? await GetDefaultAccountIdAsync("1450", "Inventory In-Transit", cancellationToken)
            : await ResolveVarianceAccountIdAsync(issue.ReasonCode, cancellationToken);
        if (!debitAccountResult.IsSuccess)
            return Result.Failure(debitAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput> { new(debitAccountResult.Value, null, totalValue, 0, null) };
        lines.AddRange(creditsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, null, 0, kv.Value, null)));

        var description = $"Goods Issue {issue.GiNo} — {issue.ReferenceType.Replace("_", " ").ToLowerInvariant()}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(issue.BranchId, issue.GiDate, "INVENTORY", "GoodsIssue", issue.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }

    /// The GL account a reason's variance posts to, falling back to the default Inventory Variance account.
    private async Task<Result<Guid>> ResolveVarianceAccountIdAsync(string? reasonCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(reasonCode))
        {
            var glAccountId = await dbContext.AdjustmentReasons.Where(r => r.Code == reasonCode).Select(r => r.GlAccountId).FirstOrDefaultAsync(cancellationToken);
            if (Guid.TryParse(glAccountId, out var explicitAccountId))
                return Result.Success(explicitAccountId);
        }

        return await GetDefaultAccountIdAsync("5100", "Inventory Variance / Shrinkage", cancellationToken);
    }
}
