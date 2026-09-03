namespace ZARI.Application.Features.Sales.PosSale;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The stock-out + COGS posting a POS sale performs in place of the Delivery Order every other
/// Sales Invoice relies on for that (see SalesInvoice.cs's own doc comment: "no stock effect —
/// Delivery already moved stock/COGS"). POS has no Delivery step of its own — it's an instant,
/// over-the-counter sale — so this mirrors DeliveryPostingService.PostStockAndGlAsync exactly
/// (issue every line as one batch, then Dr COGS / Cr Inventory, direct — no clearing account) but
/// resolves each line's warehouse from that item's own ItemBranchSetting.DefaultWarehouseId instead
/// of one header-level WarehouseId, since a POS branch's items aren't guaranteed to share a single
/// warehouse the way a Delivery's are. Deliberately kept out of CreateSalesInvoiceCommandHandler
/// itself — that handler is shared with the regular admin Sales Invoice form, where "no stock
/// effect" stays the correct, unchanged behavior; only CreatePosSaleCommandHandler calls this.
///
/// Once this posts, a Sales Return against a POS-originated line already reverses correctly with no
/// changes needed there: SalesReturnPostingService.ResolveUnitCostsAsync already falls back to the
/// item's current branch/warehouse average cost for any line with no DeliveryOrderLineId, which is
/// exactly what a POS-sold line's return line looks like — that fallback path was unreachable
/// (silently wrong, since nothing had actually deducted the stock it was "reversing") until now.
///
/// A serialized item's line also gets its specific unit marked SOLD here (IssueSerialCommand) —
/// separate from, and in addition to, the aggregate quantity move above.
/// </summary>
internal static class PosStockPostingService
{
    public static async Task<Result> PostStockAndCogsAsync(
        IAppDbContext dbContext,
        ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
        ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        Guid salesInvoiceId,
        IReadOnlyDictionary<Guid, Guid> warehouseIdByItemId,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(i => i.Id == salesInvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{salesInvoiceId}' was not found."));

        // A non-stocked item's line (e.g. a service) is silently skipped by IssueStockLinesCommand
        // itself — warehouseIdByItemId only ever needs (and only ever has) an entry for stocked
        // items, so a default of Guid.Empty here is never actually read for anything else.
        var issueLines = invoice.Lines.Select(line => new IssueStockLineItem(
            line.ItemId, invoice.BranchId, warehouseIdByItemId.GetValueOrDefault(line.ItemId), null, line.Qty,
            "SalesInvoiceLine", line.Id.ToString(), invoice.InvoiceDate, null)).ToList();

        var issueResult = await issueStockLinesHandler.HandleAsync(new IssueStockLinesCommand(issueLines), cancellationToken);
        if (!issueResult.IsSuccess)
            return Result.Failure(issueResult.Error!);

        // The aggregate quantity above is what actually moves the stock balance/costing; this marks
        // *which* physical unit left for a serialized item — CreatePosSaleCommandHandler already
        // verified every serialized line carries a SerialNo before the invoice was even created, so
        // this is never skipped for a serialized line the way a non-stocked line's cost lookup is.
        foreach (var line in invoice.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            var serialResult = await issueSerialHandler.HandleAsync(new IssueSerialCommand(line.ItemId, line.SerialNo, "SOLD"), cancellationToken);
            if (!serialResult.IsSuccess)
                return Result.Failure(serialResult.Error!);
        }

        return await PostCogsJournalAsync(dbContext, postGlJournalHandler, invoice, issueResult.Value!.CostsByReferenceId, cancellationToken);
    }

    private static async Task<Result> PostCogsJournalAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        SalesInvoice invoice,
        IReadOnlyDictionary<string, decimal> costsByReferenceId,
        CancellationToken cancellationToken)
    {
        var debitsByAccount = new Dictionary<Guid, decimal>();
        var creditsByAccount = new Dictionary<Guid, decimal>();

        foreach (var line in invoice.Lines)
        {
            // A non-stocked line has no entry in costsByReferenceId (IssueStockLinesCommand skipped
            // it entirely) — GetValueOrDefault gives 0, so it correctly contributes nothing to COGS.
            var cost = costsByReferenceId.GetValueOrDefault(line.Id.ToString());
            if (cost <= 0) continue;

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

            var amount = Math.Round(line.Qty * cost, 4);
            debitsByAccount[cogsAccountResult.Value] = debitsByAccount.GetValueOrDefault(cogsAccountResult.Value) + amount;
            creditsByAccount[inventoryAccountResult.Value] = creditsByAccount.GetValueOrDefault(inventoryAccountResult.Value) + amount;
        }

        var totalValue = debitsByAccount.Values.Sum();
        if (totalValue <= 0)
            return Result.Success();

        var lines = debitsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, invoice.CostCenterId, kv.Value, 0, null))
            .Concat(creditsByAccount.Select(kv => new PostGlJournalLineInput(kv.Key, invoice.CostCenterId, 0, kv.Value, null)))
            .ToList();

        // Same (ReferenceTable, ReferenceId) pair SalesInvoicePostingService's own AR/Revenue
        // journal uses for this invoice — GlJournal only indexes that pair, no uniqueness
        // constraint, so two journals sharing it (distinguished by description) is an accepted,
        // pre-existing pattern (e.g. a cancellation reversal journal does the same).
        var description = $"POS Sale {invoice.InvoiceNo} — Cost of Goods Sold";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(invoice.BranchId, invoice.InvoiceDate, "SALES", "SalesInvoice", invoice.Id.ToString(), description, lines), cancellationToken);
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
