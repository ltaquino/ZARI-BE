namespace ZARI.Application.Features.Inventory.StockLedgers.Issue;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Posts every line of a stock-out document (Goods Issue, a negative Adjustment/Opname variance)
/// as one batch. Validates the aggregate demand per (item, warehouse, batch) — and, separately,
/// per (item, warehouse) net of active reservations — against the just-locked balances BEFORE
/// mutating anything, so a shortfall on one line can't leave the batch half-posted. Faithfully
/// mirrors the FE prototype engine (ZARI-FE/src/data/inventory/stockLedger.ts issueStockLines),
/// just now under an explicit transaction with the (Item, Warehouse) rows locked for the duration
/// — see StockBalanceLocker for why that's the one lock this whole operation needs.
/// </summary>
public sealed class IssueStockLinesCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>
{
    public async Task<Result<IssueStockLinesResponse>> HandleAsync(IssueStockLinesCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
            return Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        var stockedLines = command.Lines
            .Where(l => items.TryGetValue(l.ItemId, out var item) && item.IsStocked)
            .ToList();

        if (stockedLines.Count == 0)
            return Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()));

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs this whole delegate — clear anything the previous, failed attempt
            // left tracked (but never saved) so we start from a clean slate.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var pairs = stockedLines.Select(l => (l.ItemId, l.WarehouseId)).Distinct().ToList();
            var lockedRows = await StockBalanceLocker.LockItemWarehousePairsAsync(dbContext, pairs, cancellationToken);

            var warehouseIds = pairs.Select(p => p.WarehouseId).Distinct().ToList();
            var costLayers = await dbContext.CostLayers
                .Where(l => itemIds.Contains(l.ItemId) && warehouseIds.Contains(l.WarehouseId) && l.QtyRemaining > 0)
                .ToListAsync(cancellationToken);

            // --- 1. Validate aggregate demand per (item, warehouse, batch) against locked balances ---
            var demandByKey = new Dictionary<(Guid ItemId, Guid WarehouseId, string? BatchNo), decimal>();
            foreach (var line in stockedLines)
            {
                var key = (line.ItemId, line.WarehouseId, StockBalanceLocker.NormalizeBatch(line.BatchNo));
                demandByKey[key] = demandByKey.GetValueOrDefault(key) + line.Qty;
            }

            foreach (var (key, qty) in demandByKey)
            {
                var onHand = StockBalanceLocker.CandidateBalances(lockedRows, key.ItemId, key.WarehouseId, key.BatchNo).Sum(b => b.QtyOnHand);
                if (onHand < qty)
                {
                    var code = items[key.ItemId].Code;
                    return Result.Failure<IssueStockLinesResponse>(Error.Validation(
                        "StockLedger.InsufficientStock",
                        $"Insufficient stock for {code} (on hand: {onHand}, requested: {qty})."));
                }
            }

            // --- 2. Validate demand per (item, warehouse) net of active reservations — a
            // reservation earmarks qty at the (item, warehouse) grain, not a specific batch, so
            // this aggregates demand across every batch-key for the same item+warehouse rather
            // than checking each batch-key's reservation share independently (which would
            // double-subtract). ---
            var demandByItemWarehouse = new Dictionary<(Guid ItemId, Guid WarehouseId), decimal>();
            foreach (var (key, qty) in demandByKey)
            {
                var iwKey = (key.ItemId, key.WarehouseId);
                demandByItemWarehouse[iwKey] = demandByItemWarehouse.GetValueOrDefault(iwKey) + qty;
            }

            var activeReservations = await dbContext.StockReservations
                .Where(r => r.Status == "ACTIVE" && itemIds.Contains(r.ItemId))
                .ToListAsync(cancellationToken);
            var reservedByItemWarehouse = activeReservations
                .GroupBy(r => (r.ItemId, r.WarehouseId))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.QtyReserved));

            foreach (var (iwKey, qty) in demandByItemWarehouse)
            {
                var totalOnHand = lockedRows.Where(b => b.ItemId == iwKey.ItemId && b.WarehouseId == iwKey.WarehouseId).Sum(b => b.QtyOnHand);
                var reserved = reservedByItemWarehouse.GetValueOrDefault(iwKey);
                var availableForIssue = Math.Max(totalOnHand - reserved, 0);
                if (availableForIssue < qty)
                {
                    var code = items[iwKey.ItemId].Code;
                    return Result.Failure<IssueStockLinesResponse>(Error.Validation(
                        "StockLedger.InsufficientAvailableStock",
                        $"Insufficient available stock for {code} after reservations (available: {availableForIssue}, requested: {qty})."));
                }
            }

            // --- 3. Apply each line, mutating the locked rows/layers as we go so later lines in
            // the same batch see what earlier lines already consumed — same order-dependent
            // behavior as the FE engine being ported. ---
            var costsByReferenceId = new Dictionary<string, decimal>();
            var uomCodeCache = new Dictionary<Guid, string?>();

            foreach (var line in stockedLines)
            {
                var item = items[line.ItemId];
                var batchNo = StockBalanceLocker.NormalizeBatch(line.BatchNo);

                if (!uomCodeCache.TryGetValue(item.BaseUomId, out var uomCode))
                {
                    uomCode = await dbContext.Uoms.Where(u => u.Id == item.BaseUomId).Select(u => u.Code).FirstOrDefaultAsync(cancellationToken);
                    uomCodeCache[item.BaseUomId] = uomCode;
                }

                var applyResult = item.CostingMethod == "Fifo"
                    ? ApplyFifoIssue(lockedRows, costLayers, item, line, batchNo)
                    : ApplyAvgIssue(lockedRows, item, line, batchNo);

                if (applyResult.IsFailure)
                    return Result.Failure<IssueStockLinesResponse>(applyResult.Error!);

                var (unitCost, consumptionsJson, balanceDrawsJson, runningQty, runningValue) = applyResult.Value;

                dbContext.StockLedgers.Add(new StockLedger
                {
                    ItemId = line.ItemId,
                    ItemCode = item.Code,
                    ItemName = item.Name,
                    UomCode = uomCode,
                    BranchId = line.BranchId,
                    WarehouseId = line.WarehouseId,
                    BatchNo = batchNo,
                    TransactionType = line.TransactionType ?? "GOODS_ISSUE",
                    ReferenceTable = line.ReferenceTable,
                    ReferenceId = line.ReferenceId,
                    QtyIn = 0,
                    QtyOut = line.Qty,
                    UnitCost = unitCost,
                    RunningBalanceQty = runningQty,
                    RunningBalanceValue = runningValue,
                    IsReversal = false,
                    ConsumptionsJson = consumptionsJson,
                    BalanceDrawsJson = balanceDrawsJson,
                    TransactionDate = line.TransactionDate,
                    PostedAt = DateTimeOffset.UtcNow
                });

                costsByReferenceId[line.ReferenceId] = unitCost;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new IssueStockLinesResponse(costsByReferenceId));
        });
    }

    private readonly record struct IssueLineResult(decimal UnitCost, string? ConsumptionsJson, string? BalanceDrawsJson, decimal RunningQty, decimal RunningValue);

    private static Result<IssueLineResult> ApplyFifoIssue(
        List<StockBalance> lockedRows,
        List<CostLayer> costLayers,
        Item item,
        IssueStockLineItem line,
        string? batchNo)
    {
        var candidates = StockBalanceLocker.CandidateBalances(lockedRows,line.ItemId, line.WarehouseId, batchNo);
        var candidateBatchKeys = candidates.Select(b => StockBalanceLocker.NormalizeBatch(b.BatchNo) ?? "").ToHashSet();

        var eligibleLayers = costLayers
            .Where(l => l.ItemId == line.ItemId && l.WarehouseId == line.WarehouseId
                && candidateBatchKeys.Contains(StockBalanceLocker.NormalizeBatch(l.BatchNo) ?? "")
                && l.QtyRemaining > 0)
            .OrderBy(l => l.ReceiptDate)
            .ThenBy(l => l.Id)
            .ToList();

        var remaining = line.Qty;
        decimal totalCost = 0;
        var used = new List<ConsumptionDto>();
        foreach (var layer in eligibleLayers)
        {
            if (remaining <= 0) break;
            var take = Math.Min(layer.QtyRemaining, remaining);
            used.Add(new ConsumptionDto(layer.Id, take));
            totalCost += take * layer.UnitCost;
            remaining -= take;
            layer.QtyRemaining -= take;
        }

        if (remaining > 0.0001m)
        {
            return Result.Failure<IssueLineResult>(Error.Failure(
                "StockLedger.InsufficientCostLayers",
                $"Insufficient FIFO cost layers for {item.Code} to cover this quantity — stock data may be out of sync."));
        }

        var unitCost = totalCost / line.Qty;

        var drawnByBatchKey = new Dictionary<string, decimal>();
        foreach (var consumption in used)
        {
            var layer = eligibleLayers.First(l => l.Id == consumption.LayerId);
            var key = StockBalanceLocker.NormalizeBatch(layer.BatchNo) ?? "";
            drawnByBatchKey[key] = drawnByBatchKey.GetValueOrDefault(key) + consumption.Qty;
        }

        foreach (var balance in candidates)
        {
            var key = StockBalanceLocker.NormalizeBatch(balance.BatchNo) ?? "";
            if (!drawnByBatchKey.TryGetValue(key, out var drawn) || drawn <= 0) continue;
            var newQty = balance.QtyOnHand - drawn;
            var newValue = balance.TotalValue - drawn * unitCost;
            balance.QtyOnHand = newQty;
            balance.TotalValue = newValue;
            balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
            balance.LastMovementDate = line.TransactionDate;
        }

        var postTotals = StockBalanceLocker.CandidateBalances(lockedRows,line.ItemId, line.WarehouseId, batchNo);
        var runningQty = postTotals.Sum(b => b.QtyOnHand);
        var runningValue = postTotals.Sum(b => b.TotalValue);

        return Result.Success(new IssueLineResult(
            unitCost,
            JsonSerializer.Serialize(used),
            null,
            runningQty,
            runningValue));
    }

    private static Result<IssueLineResult> ApplyAvgIssue(List<StockBalance> lockedRows, Item item, IssueStockLineItem line, string? batchNo)
    {
        var candidates = StockBalanceLocker.CandidateBalances(lockedRows,line.ItemId, line.WarehouseId, batchNo);

        var remaining = line.Qty;
        decimal totalCost = 0;
        var draws = new List<BalanceDrawDto>();
        foreach (var balance in candidates)
        {
            if (remaining <= 0.0001m) break;
            var take = Math.Min(balance.QtyOnHand, remaining);
            draws.Add(new BalanceDrawDto(balance.BatchNo, take, balance.AvgUnitCost));
            totalCost += take * balance.AvgUnitCost;
            remaining -= take;

            var newQty = balance.QtyOnHand - take;
            var newValue = balance.TotalValue - take * balance.AvgUnitCost;
            balance.QtyOnHand = newQty;
            balance.TotalValue = newValue;
            balance.AvgUnitCost = newQty > 0 ? newValue / newQty : balance.AvgUnitCost;
            balance.LastMovementDate = line.TransactionDate;
        }

        var unitCost = line.Qty > 0 ? totalCost / line.Qty : 0;

        var postTotals = StockBalanceLocker.CandidateBalances(lockedRows,line.ItemId, line.WarehouseId, batchNo);
        var runningQty = postTotals.Sum(b => b.QtyOnHand);
        var runningValue = postTotals.Sum(b => b.TotalValue);

        return Result.Success(new IssueLineResult(
            unitCost,
            null,
            JsonSerializer.Serialize(draws),
            runningQty,
            runningValue));
    }
}
