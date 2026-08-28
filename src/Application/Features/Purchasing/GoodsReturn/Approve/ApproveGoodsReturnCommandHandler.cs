namespace ZARI.Application.Features.Purchasing.GoodsReturns.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Purchasing.GoodsReturns.Create;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Application.Features.Purchasing.GoodsReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Mirror-image of ApproveGoodsReceiptPoCommandHandler: instead of
/// receiving stock, this issues it back out to the vendor as one batched call to the shared
/// IssueStockLinesCommand engine (the same engine GoodsIssue's approve uses), then posts the GRNI
/// reversal journal — Dr "2100" GRNI, Cr the item's inventory account. A Goods Return's UnitCost is
/// user-entered (matching what was originally received on the GRPO), so — unlike GoodsIssue, which
/// backfills line.UnitCost from IssueStockLinesResponse.CostsByReferenceId for FIFO/Avg costing —
/// there's nothing to backfill here; the response is only checked for success.
/// </summary>
public sealed class ApproveGoodsReturnCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveGoodsReturnCommand, Result<GoodsReturnResponse>>
{
    public async Task<Result<GoodsReturnResponse>> HandleAsync(ApproveGoodsReturnCommand command, CancellationToken cancellationToken = default)
    {
        var goodsReturn = await dbContext.GoodsReturns
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (goodsReturn is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("GoodsReturn.NotFound", $"Goods return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Approve, goodsReturn.BranchId, cancellationToken))
            return Result.Failure<GoodsReturnResponse>(Error.Forbidden("GoodsReturn.Forbidden", "You do not have permission to approve goods returns for this branch."));

        if (goodsReturn.Status != "PENDING_APPROVAL")
            return Result.Failure<GoodsReturnResponse>(Error.Validation("GoodsReturn.NotPendingApproval", "Only goods returns pending approval can be approved."));

        // Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
        // goods return against the same goods receipt (PO) line may have been approved in between.
        // This return is still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from
        // its own "already returned" tally — same pattern as ApprovePurchaseOrderCommandHandler.
        if (goodsReturn.GoodsReceiptPoId is not null)
        {
            var goodsReceiptPo = await dbContext.GoodsReceiptPos
                .Include(g => g.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(g => g.Id == goodsReturn.GoodsReceiptPoId, cancellationToken);
            if (goodsReceiptPo is not null)
            {
                var referencedLineIds = goodsReturn.Lines.Where(l => l.GoodsReceiptPoLineId.HasValue).Select(l => l.GoodsReceiptPoLineId!.Value).Distinct().ToList();
                var alreadyReturned = await dbContext.GoodsReturnLines
                    .Where(l => l.GoodsReceiptPoLineId.HasValue && referencedLineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.GoodsReturn.Status == "POSTED")
                    .GroupBy(l => l.GoodsReceiptPoLineId!.Value)
                    .Select(g => new { GoodsReceiptPoLineId = g.Key, Qty = g.Sum(l => l.QtyReturned) })
                    .ToDictionaryAsync(x => x.GoodsReceiptPoLineId, x => x.Qty, cancellationToken);

                var lineInputs = goodsReturn.Lines.Select(l => new GoodsReturnLineInput(l.ItemId, l.BatchNo, l.SerialNo, l.QtyReturned, l.UomId, l.UnitCost, l.GoodsReceiptPoLineId)).ToList();
                var validationResult = CreateGoodsReturnCommandHandler.ValidateAgainstGoodsReceiptPo(goodsReceiptPo, lineInputs, alreadyReturned);
                if (!validationResult.IsSuccess)
                    return Result.Failure<GoodsReturnResponse>(validationResult.Error!);
            }
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "GOODS_RETURNS" && r.EntityId == goodsReturn.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<GoodsReturnResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this goods return."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(decideResult.Error!);

        var issueLines = goodsReturn.Lines.Select(line => new IssueStockLineItem(
            line.ItemId, goodsReturn.BranchId, goodsReturn.WarehouseId, line.BatchNo, line.QtyReturned,
            "GoodsReturnLine", line.Id.ToString(), goodsReturn.ReturnDate, null)).ToList();

        var issueStockResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(issueLines), cancellationToken);
        if (!issueStockResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(issueStockResult.Error!);

        foreach (var line in goodsReturn.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await issueSerialHandler.HandleAsync(new IssueSerialCommand(line.ItemId, line.SerialNo, "DISPOSED"), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<GoodsReturnResponse>(serialResult.Error!);
        }

        var journalResult = await PostGrniReversalJournalAsync(goodsReturn, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(journalResult.Error!);

        // IssueStockLinesCommand runs its own retryable transaction and calls ChangeTracker.Clear()
        // at the start of every attempt — that detaches the `goodsReturn` this handler loaded
        // earlier, so mutating it and calling SaveChangesAsync would silently persist nothing.
        // ExecuteUpdateAsync writes directly, independent of whatever the tracker currently holds.
        goodsReturn.Status = "POSTED";
        await dbContext.GoodsReturns.Where(r => r.Id == goodsReturn.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_RETURNS", goodsReturn.Id.ToString(), goodsReturn.BranchId, "APPROVED", "ACTIVITY",
                "approved this goods return", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsReturnResponse>(notifyResult.Error!);

        return Result.Success(GoodsReturnMapper.ToResponse(goodsReturn));
    }

    private async Task<Result> PostGrniReversalJournalAsync(GoodsReturn goodsReturn, CancellationToken cancellationToken)
    {
        var creditsByAccount = new Dictionary<Guid, decimal>();
        foreach (var line in goodsReturn.Lines)
        {
            var accountResult = Guid.TryParse(line.Item.InventoryAccountId, out var explicitAccountId)
                ? Result.Success(explicitAccountId)
                : await GetDefaultAccountIdAsync("1400", "Inventory Asset", cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.Error!);

            var amount = Math.Round(line.QtyReturned * line.UnitCost, 4);
            creditsByAccount[accountResult.Value] = creditsByAccount.GetValueOrDefault(accountResult.Value) + amount;
        }

        var totalValue = creditsByAccount.Values.Sum();
        if (totalValue <= 0)
            return Result.Success();

        var grniAccountResult = await GetDefaultAccountIdAsync("2100", "Goods Received Not Invoiced", cancellationToken);
        if (!grniAccountResult.IsSuccess)
            return Result.Failure(grniAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput> { new(grniAccountResult.Value, goodsReturn.CostCenterId, totalValue, 0, null) };
        lines.AddRange(creditsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, goodsReturn.CostCenterId, 0, kv.Value, null)));

        var description = $"Goods Return {goodsReturn.ReturnNo} — {goodsReturn.Supplier.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(goodsReturn.BranchId, goodsReturn.ReturnDate, "PURCHASING", "GoodsReturn", goodsReturn.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
