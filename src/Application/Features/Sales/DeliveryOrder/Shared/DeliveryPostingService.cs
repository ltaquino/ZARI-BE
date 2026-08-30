namespace ZARI.Application.Features.Sales.DeliveryOrders.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The actual stock-out + GL posting a Delivery performs — issue every line from the stock ledger
/// as one batch (mirrors ApproveGoodsIssueCommandHandler), snapshot the costing engine's resulting
/// unit cost onto each line, then post one GL journal (Dr COGS / Cr Inventory, direct — no clearing
/// account, per SalesModuleContext.md's Delivery-&gt;Invoice GL timing decision).
///
/// Extracted here (rather than living inside ApproveDeliveryOrderCommandHandler) because it's the
/// one piece of real work both Approve AND a quick-post Create must perform identically — this is
/// the "Create-time-quick-post-vs-Approve-time shared-method" pattern this wave establishes for the
/// rest of Sales. Callers are responsible for the surrounding workflow (ApprovalRequest decide,
/// permission checks, status transitions) — this only does the stock/GL side effect.
/// </summary>
internal static class DeliveryPostingService
{
    public static async Task<Result> PostStockAndGlAsync(
        IAppDbContext dbContext,
        ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        DeliveryOrder order,
        CancellationToken cancellationToken)
    {
        var issueLines = order.Lines.Select(line => new IssueStockLineItem(
            line.ItemId, order.BranchId, order.WarehouseId, null, line.QtyShipped,
            "DeliveryOrderLine", line.Id.ToString(), order.DeliveryDate, null)).ToList();

        var issueResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(issueLines), cancellationToken);
        if (!issueResult.IsSuccess)
            return Result.Failure(issueResult.Error!);

        // IssueStockLinesCommand runs its own retryable transaction and calls ChangeTracker.Clear()
        // at the start of every attempt — that detaches the `order`/lines the caller loaded earlier,
        // so mutating them and calling SaveChangesAsync would silently persist nothing.
        // ExecuteUpdateAsync writes directly, independent of whatever the tracker currently holds.
        var costsByReferenceId = issueResult.Value!.CostsByReferenceId;
        foreach (var line in order.Lines)
        {
            if (!costsByReferenceId.TryGetValue(line.Id.ToString(), out var cost)) continue;
            line.UnitCost = cost;
            var lineId = line.Id;
            await dbContext.DeliveryOrderLines.Where(l => l.Id == lineId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UnitCost, cost), cancellationToken);
        }

        return await PostCogsJournalAsync(dbContext, postGlJournalHandler, order, cancellationToken);
    }

    private static async Task<Result> PostCogsJournalAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        DeliveryOrder order,
        CancellationToken cancellationToken)
    {
        var debitsByAccount = new Dictionary<Guid, decimal>();
        var creditsByAccount = new Dictionary<Guid, decimal>();

        foreach (var line in order.Lines)
        {
            var cogsAccountResult = Guid.TryParse(line.Item.CogsAccountId, out var explicitCogsId)
                ? Result.Success(explicitCogsId)
                : await GetDefaultAccountIdAsync(dbContext, "5000", "Cost of Goods Sold", cancellationToken);
            if (!cogsAccountResult.IsSuccess)
                return Result.Failure(cogsAccountResult.Error!);

            var inventoryAccountResult = Guid.TryParse(line.Item.InventoryAccountId, out var explicitInventoryId)
                ? Result.Success(explicitInventoryId)
                : await GetDefaultAccountIdAsync(dbContext, "1400", "Inventory Asset", cancellationToken);
            if (!inventoryAccountResult.IsSuccess)
                return Result.Failure(inventoryAccountResult.Error!);

            var amount = Math.Round(line.QtyShipped * line.UnitCost, 4);
            debitsByAccount[cogsAccountResult.Value] = debitsByAccount.GetValueOrDefault(cogsAccountResult.Value) + amount;
            creditsByAccount[inventoryAccountResult.Value] = creditsByAccount.GetValueOrDefault(inventoryAccountResult.Value) + amount;
        }

        var totalValue = debitsByAccount.Values.Sum();
        if (totalValue <= 0)
            return Result.Success();

        var lines = debitsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, order.CostCenterId, kv.Value, 0, null))
            .Concat(creditsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, order.CostCenterId, 0, kv.Value, null)))
            .ToList();

        var description = $"Delivery {order.DoNo} — {order.Customer.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(order.BranchId, order.DeliveryDate, "SALES", "DeliveryOrder", order.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private static async Task<Result<Guid>> GetDefaultAccountIdAsync(IAppDbContext dbContext, string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
