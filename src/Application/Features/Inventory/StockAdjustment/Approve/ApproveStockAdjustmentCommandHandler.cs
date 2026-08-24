namespace ZARI.Application.Features.Inventory.StockAdjustments.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Features.Inventory.StockAdjustments.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Approving IS the "checked by" record. Mirrors the FE prototype's
/// approveStockAdjustment orchestration: positive-variance lines post like a receipt (one
/// ReceiveStockCommand call each, keeping the encoder's own unit cost); negative-variance lines
/// post like an issue (one batched IssueStockLinesCommand call, cost derived by the costing
/// engine) — both tagged STOCK_ADJUSTMENT in the ledger. Reuses every already-migrated engine via
/// injected handlers, same composition pattern as Approve(Goods Receipt|Goods Issue).
/// </summary>
public sealed class ApproveStockAdjustmentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>> receiveSerialHandler,
    ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<ApproveStockAdjustmentCommand, Result<StockAdjustmentResponse>>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(ApproveStockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (adjustment is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("StockAdjustment.NotFound", $"Stock adjustment with ID '{command.Id}' was not found."));

        if (adjustment.Status != "PENDING_APPROVAL")
            return Result.Failure<StockAdjustmentResponse>(Error.Validation("StockAdjustment.NotPendingApproval", "Only stock adjustments pending approval can be approved."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "STOCK_ADJUSTMENT" && r.EntityId == adjustment.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<StockAdjustmentResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this stock adjustment."));

        var increaseLines = adjustment.Lines.Where(l => l.VarianceQty > 0.0001m).ToList();
        var decreaseLines = adjustment.Lines.Where(l => l.VarianceQty < -0.0001m).ToList();

        foreach (var line in increaseLines)
        {
            var receiveResult = await receiveStockHandler.HandleAsync(
                new ReceiveStockCommand(line.ItemId, adjustment.BranchId, adjustment.WarehouseId, line.BatchNo, line.VarianceQty, line.UnitCost,
                    "StockAdjustmentLine", line.Id.ToString(), adjustment.AdjustmentDate, "STOCK_ADJUSTMENT"),
                cancellationToken);
            if (!receiveResult.IsSuccess)
                return Result.Failure<StockAdjustmentResponse>(receiveResult.Error!);
        }

        var decreaseItems = decreaseLines.Select(line => new IssueStockLineItem(
            line.ItemId, adjustment.BranchId, adjustment.WarehouseId, line.BatchNo, Math.Abs(line.VarianceQty),
            "StockAdjustmentLine", line.Id.ToString(), adjustment.AdjustmentDate, "STOCK_ADJUSTMENT")).ToList();

        var issueStockResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(decreaseItems), cancellationToken);
        if (!issueStockResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(issueStockResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(decideResult.Error!);

        var costsByReferenceId = issueStockResult.Value!.CostsByReferenceId;
        foreach (var line in decreaseLines)
        {
            if (costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost))
                line.UnitCost = cost;
        }

        foreach (var line in increaseLines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await receiveSerialHandler.HandleAsync(new ReceiveSerialCommand(line.ItemId, line.SerialNo, adjustment.WarehouseId), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<StockAdjustmentResponse>(serialResult.Error!);
        }
        foreach (var line in decreaseLines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await issueSerialHandler.HandleAsync(new IssueSerialCommand(line.ItemId, line.SerialNo, "DISPOSED"), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<StockAdjustmentResponse>(serialResult.Error!);
        }

        var journalResult = await PostInventoryJournalAsync(adjustment, increaseLines, decreaseLines, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(journalResult.Error!);

        // ReceiveStockCommand/IssueStockLinesCommand each run their own retryable transaction and
        // call ChangeTracker.Clear() at the start of every attempt — that detaches the
        // `adjustment`/lines this handler loaded earlier, so mutating them and calling
        // SaveChangesAsync would silently persist nothing. ExecuteUpdateAsync writes directly,
        // independent of whatever the tracker currently holds.
        foreach (var line in decreaseLines)
        {
            if (!costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost)) continue;
            var lineId = line.Id;
            await dbContext.StockAdjustmentLines.Where(l => l.Id == lineId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UnitCost, cost), cancellationToken);
        }

        adjustment.Status = "POSTED";
        await dbContext.StockAdjustments.Where(a => a.Id == adjustment.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, "POSTED"), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_ADJUSTMENT", adjustment.Id.ToString(), adjustment.BranchId, "APPROVED", "ACTIVITY",
                "approved this stock adjustment", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockAdjustmentResponse>(notifyResult.Error!);

        return Result.Success(StockAdjustmentMapper.ToResponse(adjustment));
    }

    private async Task<Result> PostInventoryJournalAsync(
        StockAdjustment adjustment, List<StockAdjustmentLine> increaseLines, List<StockAdjustmentLine> decreaseLines, CancellationToken cancellationToken)
    {
        var increaseByAccount = new Dictionary<Guid, decimal>();
        foreach (var line in increaseLines)
        {
            var accountResult = await ResolveInventoryAccountIdAsync(line, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.Error!);

            var amount = Math.Round(line.VarianceQty * line.UnitCost, 4);
            increaseByAccount[accountResult.Value] = increaseByAccount.GetValueOrDefault(accountResult.Value) + amount;
        }

        var decreaseByAccount = new Dictionary<Guid, decimal>();
        foreach (var line in decreaseLines)
        {
            var accountResult = await ResolveInventoryAccountIdAsync(line, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.Error!);

            var amount = Math.Round(Math.Abs(line.VarianceQty) * line.UnitCost, 4);
            decreaseByAccount[accountResult.Value] = decreaseByAccount.GetValueOrDefault(accountResult.Value) + amount;
        }

        var increaseTotal = increaseByAccount.Values.Sum();
        var decreaseTotal = decreaseByAccount.Values.Sum();
        if (increaseTotal <= 0 && decreaseTotal <= 0)
            return Result.Success();

        var varianceAccountResult = await ResolveVarianceAccountIdAsync(adjustment.ReasonCode, cancellationToken);
        if (!varianceAccountResult.IsSuccess)
            return Result.Failure(varianceAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>();
        lines.AddRange(increaseByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, null, kv.Value, 0, null)));
        lines.AddRange(decreaseByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, null, 0, kv.Value, null)));
        if (increaseTotal > 0) lines.Add(new PostGlJournalLineInput(varianceAccountResult.Value, null, 0, increaseTotal, null));
        if (decreaseTotal > 0) lines.Add(new PostGlJournalLineInput(varianceAccountResult.Value, null, decreaseTotal, 0, null));

        var description = $"Stock Adjustment {adjustment.AdjustmentNo}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(adjustment.BranchId, adjustment.AdjustmentDate, "StockAdjustment", adjustment.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> ResolveInventoryAccountIdAsync(StockAdjustmentLine line, CancellationToken cancellationToken)
    {
        return Guid.TryParse(line.Item.InventoryAccountId, out var explicitAccountId)
            ? Result.Success(explicitAccountId)
            : await GetDefaultAccountIdAsync("1400", "Inventory Asset", cancellationToken);
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
