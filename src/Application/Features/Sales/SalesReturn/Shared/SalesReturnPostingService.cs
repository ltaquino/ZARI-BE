namespace ZARI.Application.Features.Sales.SalesReturns.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The actual stock-in + combined GL reversal a Sales Return performs — mirrors
/// ApproveGoodsReceiptPoCommandHandler's receiving loop (one ReceiveStockCommand per line, not
/// batched — receiving takes an explicit UnitCost, it isn't computed by a costing engine the way
/// issuing is) for the stock side, and reuses SalesInvoiceLineCalculator.SplitVat for the
/// revenue-side reversal. One balanced journal covers both halves in a single PostGlJournalCommand:
/// Dr Inventory / Cr COGS for the stock-value reversal, plus Dr "4100" Sales Returns &amp;
/// Allowances + Dr "2200" VAT Payable / Cr AR for the credit-memo reversal, at the original sale's
/// own VAT treatment (a return never recomputes a new discount/VAT decision).
///
/// Extracted here (rather than living inside ApproveSalesReturnCommandHandler) so both a quick-post
/// Create and a normal Approve run the exact same posting — same "Create-time-quick-post-vs-
/// Approve-time shared-method" pattern DeliveryPostingService/SalesInvoicePostingService established.
///
/// Also reverses a serialized item's SOLD status back to IN_STOCK (ReverseIssueSerialCommand) when
/// the line carries a SerialNo — best-effort, since only a POS-originated line ever had one
/// recorded as sold in the first place; see SalesReturnLine.SerialNo's own doc comment.
/// </summary>
internal static class SalesReturnPostingService
{
    /// <summary>
    /// <paramref name="manualVatTypeByLineId"/> carries the caller-supplied VatType for any line with
    /// no DeliveryOrderLineId. SalesReturnLine has no column to persist this (the entity has no spare
    /// field, and this build is not allowed to add one via a migration — see the Sales Return build
    /// notes), so it only survives from a quick-post Create's own command input, passed straight
    /// through in the same call. A normal Submit/Approve flow has nothing to pass here (pass null or
    /// an empty dictionary) — any non-delivery-referenced line then falls back to the item's own
    /// default VatType at Approve time. This is a known v1 simplification: a future wave that wants
    /// the manually-chosen VAT treatment to survive to Approve would need to add a persisted column.
    /// </summary>
    public static async Task<Result> PostAsync(
        IAppDbContext dbContext,
        ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveStockHandler,
        ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        SalesReturn salesReturn,
        IReadOnlyDictionary<Guid, string>? manualVatTypeByLineId,
        CancellationToken cancellationToken)
    {
        var unitCostByLineId = await ResolveUnitCostsAsync(dbContext, salesReturn, cancellationToken);
        var vatTypeByLineId = await ResolveVatTypesAsync(dbContext, salesReturn, manualVatTypeByLineId, cancellationToken);

        foreach (var line in salesReturn.Lines)
        {
            var receiveResult = await receiveStockHandler.HandleAsync(
                new ReceiveStockCommand(line.ItemId, salesReturn.BranchId, salesReturn.WarehouseId, null, line.QtyReturned,
                    unitCostByLineId[line.Id], "SalesReturnLine", line.Id.ToString(), salesReturn.ReturnDate, null),
                cancellationToken);
            if (!receiveResult.IsSuccess)
                return Result.Failure(receiveResult.Error!);

            // Best-effort: only a POS-originated line ever had a serial recorded as sold in the
            // first place (a Delivery-linked line has nothing to reverse — Delivery Order doesn't
            // track serials at all, a separate known gap). Absent SerialNo, this is a total no-op,
            // identical to this return's behavior before SerialNo existed.
            if (line.Item.IsSerialized && !string.IsNullOrWhiteSpace(line.SerialNo))
            {
                var reverseSerialResult = await reverseIssueSerialHandler.HandleAsync(new ReverseIssueSerialCommand(line.ItemId, line.SerialNo), cancellationToken);
                if (!reverseSerialResult.IsSuccess)
                    return Result.Failure(reverseSerialResult.Error!);
            }
        }

        return await PostReversalJournalAsync(dbContext, postGlJournalHandler, salesReturn, unitCostByLineId, vatTypeByLineId, cancellationToken);
    }

    /// <summary>
    /// UnitCost for the stock-value reversal is not stored on SalesReturnLine (unlike UnitPrice) —
    /// when the line references a DeliveryOrderLine, that line's own UnitCost (stamped by
    /// DeliveryPostingService from the costing engine at the time it shipped) is the exact cost the
    /// item left inventory at, so reversing at that same cost is the most accurate choice. Otherwise
    /// fall back to the item's current value-weighted average cost across every batch bucket for
    /// this (Item, Branch, Warehouse) — StockBalance is tracked per batch, so more than one row can
    /// exist for the same item/warehouse.
    /// </summary>
    private static async Task<Dictionary<Guid, decimal>> ResolveUnitCostsAsync(IAppDbContext dbContext, SalesReturn salesReturn, CancellationToken cancellationToken)
    {
        var itemIdsNeedingLookup = salesReturn.Lines.Where(l => !l.DeliveryOrderLineId.HasValue).Select(l => l.ItemId).Distinct().ToList();
        var avgCostByItemId = new Dictionary<Guid, decimal>();

        if (itemIdsNeedingLookup.Count > 0)
        {
            var balances = await dbContext.StockBalances
                .Where(b => itemIdsNeedingLookup.Contains(b.ItemId) && b.BranchId == salesReturn.BranchId && b.WarehouseId == salesReturn.WarehouseId)
                .ToListAsync(cancellationToken);

            foreach (var group in balances.GroupBy(b => b.ItemId))
            {
                var qty = group.Sum(b => b.QtyOnHand);
                var value = group.Sum(b => b.TotalValue);
                avgCostByItemId[group.Key] = qty > 0 ? value / qty : 0;
            }
        }

        return salesReturn.Lines.ToDictionary(
            l => l.Id,
            l => l.DeliveryOrderLineId.HasValue ? l.DeliveryOrderLine!.UnitCost : avgCostByItemId.GetValueOrDefault(l.ItemId));
    }

    /// <summary>
    /// A return credits at the original sale's own VAT treatment. When the line references a
    /// DeliveryOrderLine, walk to any POSTED SalesInvoiceLine that billed against that same delivery
    /// line and read its effective VAT type (a statutory-discounted original line is treated as
    /// VAT_EXEMPT, matching SalesInvoiceLineCalculator's own EffectiveVatType). If more than one
    /// invoice line ever referenced the same delivery line (partial invoicing across documents), the
    /// first one found is used — the plan doesn't anticipate mixed VAT treatment within one delivery
    /// line. If delivered but never invoiced (or no delivery reference at all and no manual override
    /// supplied), fall back to the item's own default VatType.
    /// </summary>
    private static async Task<Dictionary<Guid, string>> ResolveVatTypesAsync(
        IAppDbContext dbContext, SalesReturn salesReturn, IReadOnlyDictionary<Guid, string>? manualVatTypeByLineId, CancellationToken cancellationToken)
    {
        var deliveryLineIds = salesReturn.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
        var invoiceLinesByDeliveryLineId = deliveryLineIds.Count == 0
            ? []
            : await dbContext.SalesInvoiceLines
                .Where(l => l.DeliveryOrderLineId.HasValue && deliveryLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesInvoice.Status == "POSTED")
                .Select(l => new { l.DeliveryOrderLineId, l.VatType, l.StatutoryDiscountTypeId })
                .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, string>();
        foreach (var line in salesReturn.Lines)
        {
            if (line.DeliveryOrderLineId.HasValue)
            {
                var invoiceLine = invoiceLinesByDeliveryLineId.FirstOrDefault(l => l.DeliveryOrderLineId == line.DeliveryOrderLineId);
                result[line.Id] = invoiceLine is null
                    ? line.Item.VatType
                    : invoiceLine.StatutoryDiscountTypeId.HasValue ? "VAT_EXEMPT" : invoiceLine.VatType;
            }
            else if (manualVatTypeByLineId is not null && manualVatTypeByLineId.TryGetValue(line.Id, out var manual))
            {
                result[line.Id] = manual;
            }
            else
            {
                result[line.Id] = line.Item.VatType;
            }
        }

        return result;
    }

    /// <summary>
    /// One balanced journal: Dr Inventory (Item.InventoryAccountId ?? "1400") / Cr COGS
    /// (Item.CogsAccountId ?? "5000") for the stock-value reversal (QtyReturned x resolved UnitCost,
    /// grouped by account) — the mirror image of Delivery's Dr COGS/Cr Inventory — plus Dr "4100"
    /// Sales Returns &amp; Allowances for the summed VAT-exclusive net, Dr "2200" VAT Payable for the
    /// summed VAT (VATable lines only), and Cr AR (Customer.ArAccountId ?? "1200") for the summed
    /// gross credited back to the customer's balance. Zero-amount groups are filtered out before
    /// building the journal (a line with both Debit and Credit at 0 is rejected as malformed).
    /// </summary>
    private static async Task<Result> PostReversalJournalAsync(
        IAppDbContext dbContext,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
        SalesReturn salesReturn,
        Dictionary<Guid, decimal> unitCostByLineId,
        Dictionary<Guid, string> vatTypeByLineId,
        CancellationToken cancellationToken)
    {
        var inventoryByAccount = new Dictionary<Guid, decimal>();
        var cogsByAccount = new Dictionary<Guid, decimal>();
        decimal totalNetOfVat = 0, totalVat = 0, totalGross = 0;

        foreach (var line in salesReturn.Lines)
        {
            var stockAmount = Math.Round(line.QtyReturned * unitCostByLineId[line.Id], 4);
            if (stockAmount > 0)
            {
                var inventoryAccountResult = Guid.TryParse(line.Item.InventoryAccountId, out var explicitInventoryId)
                    ? Result.Success(explicitInventoryId)
                    : await GetDefaultAccountIdAsync(dbContext, "1400", "Inventory Asset", cancellationToken);
                if (!inventoryAccountResult.IsSuccess)
                    return Result.Failure(inventoryAccountResult.Error!);

                var cogsAccountResult = Guid.TryParse(line.Item.CogsAccountId, out var explicitCogsId)
                    ? Result.Success(explicitCogsId)
                    : await GetDefaultAccountIdAsync(dbContext, "5000", "Cost of Goods Sold", cancellationToken);
                if (!cogsAccountResult.IsSuccess)
                    return Result.Failure(cogsAccountResult.Error!);

                inventoryByAccount[inventoryAccountResult.Value] = inventoryByAccount.GetValueOrDefault(inventoryAccountResult.Value) + stockAmount;
                cogsByAccount[cogsAccountResult.Value] = cogsByAccount.GetValueOrDefault(cogsAccountResult.Value) + stockAmount;
            }

            var grossAmount = Math.Round(line.QtyReturned * line.UnitPrice, 4);
            var (netOfVat, vatAmount) = SalesInvoiceLineCalculator.SplitVat(grossAmount, vatTypeByLineId[line.Id]);
            totalNetOfVat += netOfVat;
            totalVat += vatAmount;
            totalGross += grossAmount;
        }

        totalNetOfVat = Math.Round(totalNetOfVat, 4);
        totalVat = Math.Round(totalVat, 4);
        totalGross = Math.Round(totalGross, 4);

        var lines = new List<PostGlJournalLineInput>();
        lines.AddRange(inventoryByAccount.Where(kv => kv.Value > 0)
            .Select(kv => new PostGlJournalLineInput(kv.Key, salesReturn.CostCenterId, kv.Value, 0, null)));
        lines.AddRange(cogsByAccount.Where(kv => kv.Value > 0)
            .Select(kv => new PostGlJournalLineInput(kv.Key, salesReturn.CostCenterId, 0, kv.Value, null)));

        if (totalNetOfVat > 0)
        {
            var salesReturnsAccountResult = await GetDefaultAccountIdAsync(dbContext, "4100", "Sales Returns and Allowances", cancellationToken);
            if (!salesReturnsAccountResult.IsSuccess)
                return Result.Failure(salesReturnsAccountResult.Error!);
            lines.Add(new PostGlJournalLineInput(salesReturnsAccountResult.Value, salesReturn.CostCenterId, totalNetOfVat, 0, null));
        }

        if (totalVat > 0)
        {
            var vatAccountResult = await GetDefaultAccountIdAsync(dbContext, "2200", "VAT Payable", cancellationToken);
            if (!vatAccountResult.IsSuccess)
                return Result.Failure(vatAccountResult.Error!);
            lines.Add(new PostGlJournalLineInput(vatAccountResult.Value, salesReturn.CostCenterId, totalVat, 0, null));
        }

        if (totalGross > 0)
        {
            var arAccountResult = salesReturn.Customer.ArAccountId.HasValue
                ? Result.Success(salesReturn.Customer.ArAccountId.Value)
                : await GetDefaultAccountIdAsync(dbContext, "1200", "Accounts Receivable", cancellationToken);
            if (!arAccountResult.IsSuccess)
                return Result.Failure(arAccountResult.Error!);
            lines.Add(new PostGlJournalLineInput(arAccountResult.Value, salesReturn.CostCenterId, 0, totalGross, null));
        }

        if (lines.Count == 0)
            return Result.Success();

        var description = $"Sales Return {salesReturn.ReturnNo} — {salesReturn.Customer.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(salesReturn.BranchId, salesReturn.ReturnDate, "SALES", "SalesReturn", salesReturn.Id.ToString(), description, lines), cancellationToken);
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
