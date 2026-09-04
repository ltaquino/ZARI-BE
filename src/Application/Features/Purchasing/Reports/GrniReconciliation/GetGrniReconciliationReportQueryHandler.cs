namespace ZARI.Application.Features.Purchasing.Reports.GrniReconciliation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Ported verbatim (including its reasoning, kept as comments below) from the FE's
/// GrniReconciliationReportPage.tsx — the most complex report in the Purchasing set, so faithfulness
/// to the original client-side logic matters more than a from-scratch rewrite.
/// </summary>
public sealed class GetGrniReconciliationReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetGrniReconciliationReportQuery, Result<GrniReconciliationReportResponse>>
{
    // A GRPO's own GL journal (Dr Inventory / Cr GRNI) stays live for as long as the receipt itself is
    // POSTED or mid-cancellation-request — only an actually CANCELLED receipt has had it reversed.
    private static readonly HashSet<string> GrpoActiveStatuses = ["POSTED", "PENDING_CANCELLATION"];

    // An AP Invoice's GRNI-clearing journal posts once, at Approve — every status after that
    // (PARTIALLY_PAID, PAID, a pending cancellation request) is still the same live journal; only
    // CANCELLED reverses it.
    private static readonly HashSet<string> InvoiceActiveStatuses = ["POSTED", "PARTIALLY_PAID", "PAID", "PENDING_CANCELLATION"];

    private static readonly HashSet<string> ReturnActiveStatuses = ["POSTED", "PENDING_CANCELLATION"];

    public async Task<Result<GrniReconciliationReportResponse>> HandleAsync(GetGrniReconciliationReportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GOODS_RECEIPT_PO", FormAction.View, cancellationToken))
            return Result.Failure<GrniReconciliationReportResponse>(Error.Forbidden("GrniReconciliationReport.Forbidden", "You do not have permission to view goods receipts (PO)."));

        // Loaded unfiltered by branch/status — needed to resolve ANY active invoice/return line back
        // to its GRPO line, regardless of which branch's report is being viewed.
        var allGrpos = await dbContext.GoodsReceiptPos.AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.Lines)
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.ApInvoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => InvoiceActiveStatuses.Contains(i.Status))
            .ToListAsync(cancellationToken);

        var returns = await dbContext.GoodsReturns.AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => ReturnActiveStatuses.Contains(r.Status))
            .ToListAsync(cancellationToken);

        // Every GRPO line knows the GRPO it belongs to — needed to roll a line-level "how much of this
        // line has been cleared" figure back up into a GRPO-level row, and to resolve legacy documents
        // (predating Phase 18's line-level FKs) by matching on item within the referenced GRPO instead.
        var grpoLineIndex = new Dictionary<Guid, (GoodsReceiptPo Grpo, GoodsReceiptPoLine Line)>();
        var lineByGrpoAndItem = new Dictionary<(Guid GrpoId, Guid ItemId), Guid>();
        foreach (var grpo in allGrpos)
        {
            foreach (var line in grpo.Lines)
            {
                grpoLineIndex[line.Id] = (grpo, line);
                lineByGrpoAndItem[(grpo.Id, line.ItemId)] = line.Id;
            }
        }

        Guid? ResolveGrpoLineId(Guid? explicitLineId, Guid? grpoId, Guid itemId)
        {
            if (explicitLineId is { } id) return id;
            if (grpoId is not { } gId) return null;
            // Legacy fallback for documents created before Phase 18 wired up per-line FKs.
            return lineByGrpoAndItem.TryGetValue((gId, itemId), out var resolved) ? resolved : null;
        }

        // GRNI clears two ways: an AP Invoice converts it to a real payable, or a Goods Return reverses
        // it straight back out. Both are qty-weighted at the GRPO line's own unit cost (matching how
        // each Approve handler actually posts), summed per GRPO line, then capped at that line's own
        // received value below — a line can never show as "more than fully cleared" from the document
        // side even if the ledger itself was over-cleared by a since-fixed bug or bad data (that's
        // exactly the kind of gap the live GL comparison below exists to surface).
        var clearedValueByGrpoLine = new Dictionary<Guid, decimal>();
        var invoiceNosByGrpoId = new Dictionary<Guid, SortedSet<string>>();
        var returnNosByGrpoId = new Dictionary<Guid, SortedSet<string>>();

        foreach (var invoice in invoices)
        {
            foreach (var line in invoice.Lines)
            {
                var grpoLineId = ResolveGrpoLineId(line.GoodsReceiptPoLineId, invoice.GoodsReceiptPoId, line.ItemId);
                if (grpoLineId is not { } lineId || !grpoLineIndex.TryGetValue(lineId, out var entry)) continue;

                clearedValueByGrpoLine[lineId] = clearedValueByGrpoLine.GetValueOrDefault(lineId) + line.Qty * entry.Line.UnitCost;
                if (!invoiceNosByGrpoId.TryGetValue(entry.Grpo.Id, out var set)) invoiceNosByGrpoId[entry.Grpo.Id] = set = [];
                set.Add(invoice.InvoiceNo);
            }
        }

        foreach (var ret in returns)
        {
            foreach (var line in ret.Lines)
            {
                var grpoLineId = ResolveGrpoLineId(line.GoodsReceiptPoLineId, ret.GoodsReceiptPoId, line.ItemId);
                if (grpoLineId is not { } lineId || !grpoLineIndex.TryGetValue(lineId, out var entry)) continue;

                clearedValueByGrpoLine[lineId] = clearedValueByGrpoLine.GetValueOrDefault(lineId) + line.QtyReturned * entry.Line.UnitCost;
                if (!returnNosByGrpoId.TryGetValue(entry.Grpo.Id, out var set)) returnNosByGrpoId[entry.Grpo.Id] = set = [];
                set.Add(ret.ReturnNo);
            }
        }

        var candidateGrpos = allGrpos.Where(g => GrpoActiveStatuses.Contains(g.Status));
        if (!string.IsNullOrWhiteSpace(query.BranchId)) candidateGrpos = candidateGrpos.Where(g => g.BranchId == query.BranchId);

        var allRows = candidateGrpos
            .Select(grpo =>
            {
                var value = grpo.Lines.Sum(l => l.QtyReceived * l.UnitCost);
                var clearedValue = grpo.Lines.Sum(l =>
                {
                    var lineValue = l.QtyReceived * l.UnitCost;
                    return Math.Min(clearedValueByGrpoLine.GetValueOrDefault(l.Id), lineValue);
                });
                SortedSet<string> invoiceNos = invoiceNosByGrpoId.GetValueOrDefault(grpo.Id) ?? [];
                SortedSet<string> returnNos = returnNosByGrpoId.GetValueOrDefault(grpo.Id) ?? [];
                return new GrniGrpoRow(
                    grpo.Id,
                    grpo.GrpoNo,
                    grpo.BranchId,
                    grpo.Supplier.Name,
                    grpo.ReceiptDate,
                    value,
                    clearedValue,
                    value - clearedValue,
                    invoiceNos.Concat(returnNos).ToList());
            })
            .OrderByDescending(r => r.ReceiptDate)
            .ToList();

        var totalReceived = allRows.Sum(r => r.Value);
        var totalOutstanding = allRows.Sum(r => r.Outstanding);
        var totalCleared = totalReceived - totalOutstanding;

        var rows = query.ShowOnlyOutstanding ? allRows.Where(r => r.Outstanding > 0.005m).ToList() : allRows;

        // The actual "2100 Goods Received Not Invoiced" ledger balance — every journal line against
        // that account, signed by its normal balance (Liability/Credit here), NO status filter (same
        // reasoning as the Trial Balance report: a genuinely reversed journal already carries the
        // opposite signed entries, so it nets itself out automatically without needing to be excluded).
        // A real gap here means the ledger and the documents disagree — e.g. a GRPO line billed twice
        // before Phase 18's quantity enforcement existed, over-clearing the ledger beyond what any
        // single document could account for — not ordinary price variance, which is already swept to
        // "5200 Purchase Price Variance" on each invoice.
        var grniAccount = await dbContext.GlAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Code == "2100", cancellationToken);

        decimal liveGrniBalance = 0m;
        if (grniAccount is not null)
        {
            var sign = grniAccount.NormalBalance == "Debit" ? 1 : -1;

            var journalsQuery = dbContext.GlJournals.AsNoTracking().Include(j => j.Lines).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.BranchId)) journalsQuery = journalsQuery.Where(j => j.BranchId == query.BranchId);
            var journals = await journalsQuery.ToListAsync(cancellationToken);

            liveGrniBalance = journals
                .SelectMany(j => j.Lines)
                .Where(l => l.AccountId == grniAccount.Id)
                .Sum(l => sign * (l.DebitAmount - l.CreditAmount));
        }

        var variance = totalOutstanding - liveGrniBalance;
        var isReconciled = Math.Abs(variance) < 0.01m;

        return Result.Success(new GrniReconciliationReportResponse(rows, totalReceived, totalCleared, totalOutstanding, liveGrniBalance, variance, isReconciled));
    }
}
