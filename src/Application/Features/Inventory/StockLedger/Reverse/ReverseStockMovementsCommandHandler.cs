namespace ZARI.Application.Features.Inventory.StockLedgers.Reverse;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class ReverseStockMovementsCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReverseStockMovementsCommand, Result>
{
    public async Task<Result> HandleAsync(ReverseStockMovementsCommand command, CancellationToken cancellationToken = default)
    {
        var originals = await dbContext.StockLedgers
            .Where(l => l.ReferenceTable == command.ReferenceTable && command.ReferenceIds.Contains(l.ReferenceId) && !l.IsReversal)
            .ToListAsync(cancellationToken);

        if (originals.Count == 0)
            return Result.Success();

        var itemIds = originals.Select(o => o.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs this whole delegate — clear anything the previous, failed attempt
            // left tracked (but never saved) so we start from a clean slate.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var pairs = originals.Select(o => (o.ItemId, o.WarehouseId)).Distinct().ToList();
            var lockedRows = await StockBalanceLocker.LockItemWarehousePairsAsync(dbContext, pairs, cancellationToken);

            var warehouseIds = pairs.Select(p => p.WarehouseId).Distinct().ToList();
            var costLayers = await dbContext.CostLayers
                .Where(l => itemIds.Contains(l.ItemId) && warehouseIds.Contains(l.WarehouseId))
                .ToListAsync(cancellationToken);

            // --- Validate every original is actually reversible BEFORE mutating any of them, so
            // a failure never leaves the batch half-reversed. ---
            foreach (var original in originals)
            {
                if (original.QtyIn <= 0) continue; // reversing an issue always just adds stock back — always safe

                var qty = original.QtyIn;
                var item = items[original.ItemId];

                if (item.CostingMethod == "Fifo")
                {
                    var layer = costLayers.FirstOrDefault(l => l.SourceReferenceTable == original.ReferenceTable && l.SourceReferenceId == original.ReferenceId);
                    if (layer is not null && layer.QtyRemaining < qty - 0.0001m)
                    {
                        return Result.Failure(Error.Validation(
                            "StockLedger.CannotReverse",
                            $"Cannot cancel this receipt: {(qty - layer.QtyRemaining):F4} unit(s) of {item.Code} from this batch have already been issued out."));
                    }
                }

                var balance = StockBalanceLocker.FindExact(lockedRows, original.ItemId, original.WarehouseId, original.BatchNo);
                if (balance is null || balance.QtyOnHand < qty - 0.0001m)
                {
                    return Result.Failure(Error.Validation(
                        "StockLedger.CannotReverse",
                        $"Cannot cancel this receipt: stock for {item.Code} has already moved below the received quantity."));
                }
            }

            foreach (var original in originals)
            {
                ApplyReversal(dbContext, lockedRows, costLayers, items[original.ItemId], original);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        });
    }

    private static void ApplyReversal(IAppDbContext dbContext, List<StockBalance> lockedRows, List<CostLayer> costLayers, Item item, StockLedger original)
    {
        var now = DateTimeOffset.UtcNow;

        if (original.QtyIn > 0)
        {
            // Receipts always write to one exact batch bucket, so there's exactly one balance to touch.
            var balance = StockBalanceLocker.FindExact(lockedRows, original.ItemId, original.WarehouseId, original.BatchNo);
            if (balance is null) return;
            var qty = original.QtyIn;

            if (item.CostingMethod == "Fifo")
            {
                var layer = costLayers.FirstOrDefault(l => l.SourceReferenceTable == original.ReferenceTable && l.SourceReferenceId == original.ReferenceId);
                if (layer is not null) layer.QtyRemaining -= qty;
            }

            var newQty = balance.QtyOnHand - qty;
            var newValue = balance.TotalValue - qty * original.UnitCost;
            balance.QtyOnHand = newQty;
            balance.TotalValue = newValue;
            balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
            balance.LastMovementDate = now;

            dbContext.StockLedgers.Add(BuildReversalRow(original, qtyIn: 0, qtyOut: qty, newQty, newValue, now));
        }
        else
        {
            // An issue can have drawn from more than one batch bucket (a blank-batch line spans
            // whatever was available) — restore each bucket it actually came from, not just one
            // guessed-at balance.
            var qty = original.QtyOut;
            var consumptions = Deserialize<List<ConsumptionDto>>(original.ConsumptionsJson);
            var balanceDraws = Deserialize<List<BalanceDrawDto>>(original.BalanceDrawsJson);

            if (item.CostingMethod == "Fifo" && consumptions is { Count: > 0 })
            {
                foreach (var c in consumptions)
                {
                    var layer = costLayers.FirstOrDefault(l => l.Id == c.LayerId);
                    if (layer is not null) layer.QtyRemaining += c.Qty;
                }

                var restoreByBatchKey = new Dictionary<string, (decimal Qty, decimal Value)>();
                foreach (var c in consumptions)
                {
                    var layer = costLayers.FirstOrDefault(l => l.Id == c.LayerId);
                    var key = StockBalanceLocker.NormalizeBatch(layer?.BatchNo) ?? "";
                    var prev = restoreByBatchKey.GetValueOrDefault(key);
                    restoreByBatchKey[key] = (prev.Qty + c.Qty, prev.Value + c.Qty * (layer?.UnitCost ?? original.UnitCost));
                }

                foreach (var (key, restore) in restoreByBatchKey)
                {
                    var balance = lockedRows.FirstOrDefault(b =>
                        b.ItemId == original.ItemId && b.WarehouseId == original.WarehouseId
                        && (StockBalanceLocker.NormalizeBatch(b.BatchNo) ?? "") == key);
                    if (balance is null) continue;
                    var newQty = balance.QtyOnHand + restore.Qty;
                    var newValue = balance.TotalValue + restore.Value;
                    balance.QtyOnHand = newQty;
                    balance.TotalValue = newValue;
                    balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
                    balance.LastMovementDate = now;
                }
            }
            else if (balanceDraws is { Count: > 0 })
            {
                foreach (var draw in balanceDraws)
                {
                    var balance = StockBalanceLocker.FindExact(lockedRows, original.ItemId, original.WarehouseId, draw.BatchNo);
                    if (balance is null) continue;
                    var newQty = balance.QtyOnHand + draw.Qty;
                    var newValue = balance.TotalValue + draw.Qty * draw.UnitCost;
                    balance.QtyOnHand = newQty;
                    balance.TotalValue = newValue;
                    balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
                    balance.LastMovementDate = now;
                }
            }
            else
            {
                // Pre-existing ledger row from before batch-agnostic issuing shipped — fall back to
                // the single exact-batch balance.
                var balance = StockBalanceLocker.FindExact(lockedRows, original.ItemId, original.WarehouseId, original.BatchNo);
                if (balance is not null)
                {
                    var newQty = balance.QtyOnHand + qty;
                    var newValue = balance.TotalValue + qty * original.UnitCost;
                    balance.QtyOnHand = newQty;
                    balance.TotalValue = newValue;
                    balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
                    balance.LastMovementDate = now;
                }
            }

            var postTotals = StockBalanceLocker.CandidateBalances(lockedRows, original.ItemId, original.WarehouseId, original.BatchNo);
            var runningQty = postTotals.Sum(b => b.QtyOnHand);
            var runningValue = postTotals.Sum(b => b.TotalValue);

            dbContext.StockLedgers.Add(BuildReversalRow(original, qtyIn: qty, qtyOut: 0, runningQty, runningValue, now));
        }
    }

    private static StockLedger BuildReversalRow(StockLedger original, decimal qtyIn, decimal qtyOut, decimal runningQty, decimal runningValue, DateTimeOffset now) => new()
    {
        ItemId = original.ItemId,
        ItemCode = original.ItemCode,
        ItemName = original.ItemName,
        UomCode = original.UomCode,
        BranchId = original.BranchId,
        WarehouseId = original.WarehouseId,
        BatchNo = original.BatchNo,
        TransactionType = original.TransactionType,
        ReferenceTable = original.ReferenceTable,
        ReferenceId = original.ReferenceId,
        QtyIn = qtyIn,
        QtyOut = qtyOut,
        UnitCost = original.UnitCost,
        RunningBalanceQty = runningQty,
        RunningBalanceValue = runningValue,
        IsReversal = true,
        TransactionDate = now,
        PostedAt = now
    };

    private static T? Deserialize<T>(string? json) where T : class
        => json is null ? null : JsonSerializer.Deserialize<T>(json);
}
