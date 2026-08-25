namespace ZARI.Application.Features.Inventory.StockOpnames.Post;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT -> POSTED. No separate approval step — a branch manager posts the count directly, since
/// the physical count itself is the evidence (matches the FRS: StockOpname has no
/// RequestedBy/ApprovedBy fields, unlike Stock Adjustment). Mirrors the FE prototype's
/// postStockOpname: positive-variance lines post like a receipt, negative-variance lines post like
/// an issue — both tagged STOCK_OPNAME in the ledger, and any variance always nets against the
/// default Inventory Variance account (no per-reason override — StockOpname has no ReasonCode).
/// </summary>
public sealed class PostStockOpnameCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>> receiveSerialHandler,
    ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<PostStockOpnameCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(PostStockOpnameCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_OPNAMES", FormAction.Approve, opname.BranchId, cancellationToken))
            return Result.Failure<StockOpnameResponse>(Error.Forbidden("StockOpname.Forbidden", "You do not have permission to post stock counts for this branch."));

        if (opname.Status != "DRAFT")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.NotDraft", "Only a draft stock count can be posted."));

        if (opname.Lines.Count == 0)
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.NoLines", "Add at least one line before posting."));

        var increaseLines = opname.Lines.Where(l => l.VarianceQty > 0.0001m).ToList();
        var decreaseLines = opname.Lines.Where(l => l.VarianceQty < -0.0001m).ToList();

        foreach (var line in increaseLines)
        {
            var receiveResult = await receiveStockHandler.HandleAsync(
                new ReceiveStockCommand(line.ItemId, opname.BranchId, opname.WarehouseId, line.BatchNo, line.VarianceQty, line.UnitCost,
                    "StockOpnameLine", line.Id.ToString(), opname.CountDate, "STOCK_OPNAME"),
                cancellationToken);
            if (!receiveResult.IsSuccess)
                return Result.Failure<StockOpnameResponse>(receiveResult.Error!);
        }

        var decreaseItems = decreaseLines.Select(line => new IssueStockLineItem(
            line.ItemId, opname.BranchId, opname.WarehouseId, line.BatchNo, Math.Abs(line.VarianceQty),
            "StockOpnameLine", line.Id.ToString(), opname.CountDate, "STOCK_OPNAME")).ToList();

        var issueStockResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(decreaseItems), cancellationToken);
        if (!issueStockResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(issueStockResult.Error!);

        var costsByReferenceId = issueStockResult.Value!.CostsByReferenceId;
        foreach (var line in decreaseLines)
        {
            if (costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost))
                line.UnitCost = cost;
        }

        foreach (var line in increaseLines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await receiveSerialHandler.HandleAsync(new ReceiveSerialCommand(line.ItemId, line.SerialNo, opname.WarehouseId), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<StockOpnameResponse>(serialResult.Error!);
        }
        foreach (var line in decreaseLines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await issueSerialHandler.HandleAsync(new IssueSerialCommand(line.ItemId, line.SerialNo, "DISPOSED"), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure<StockOpnameResponse>(serialResult.Error!);
        }

        var journalResult = await PostInventoryJournalAsync(opname, increaseLines, decreaseLines, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(journalResult.Error!);

        // ReceiveStockCommand/IssueStockLinesCommand each run their own retryable transaction and
        // call ChangeTracker.Clear() at the start of every attempt — that detaches the
        // `opname`/lines this handler loaded earlier, so mutating them and calling
        // SaveChangesAsync would silently persist nothing. ExecuteUpdateAsync writes directly,
        // independent of whatever the tracker currently holds.
        foreach (var line in decreaseLines)
        {
            if (!costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost)) continue;
            var lineId = line.Id;
            await dbContext.StockOpnameLines.Where(l => l.Id == lineId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UnitCost, cost), cancellationToken);
        }

        opname.Status = "POSTED";
        opname.PostedBy = command.PostedBy;
        await dbContext.StockOpnames.Where(o => o.Id == opname.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, "POSTED")
                .SetProperty(o => o.PostedBy, command.PostedBy), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "APPROVED", "ACTIVITY",
                "posted this stock count", command.PostedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }

    private async Task<Result> PostInventoryJournalAsync(
        StockOpname opname, List<StockOpnameLine> increaseLines, List<StockOpnameLine> decreaseLines, CancellationToken cancellationToken)
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

        var varianceAccountResult = await GetDefaultAccountIdAsync("5100", "Inventory Variance / Shrinkage", cancellationToken);
        if (!varianceAccountResult.IsSuccess)
            return Result.Failure(varianceAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>();
        lines.AddRange(increaseByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, null, kv.Value, 0, null)));
        lines.AddRange(decreaseByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, null, 0, kv.Value, null)));
        if (increaseTotal > 0) lines.Add(new PostGlJournalLineInput(varianceAccountResult.Value, null, 0, increaseTotal, null));
        if (decreaseTotal > 0) lines.Add(new PostGlJournalLineInput(varianceAccountResult.Value, null, decreaseTotal, 0, null));

        var description = $"Stock Opname {opname.OpnameNo}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(opname.BranchId, opname.CountDate, "INVENTORY", "StockOpname", opname.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> ResolveInventoryAccountIdAsync(StockOpnameLine line, CancellationToken cancellationToken)
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
}
