namespace ZARI.Application.Features.Inventory.StockLedgers.Receive;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class ReceiveStockCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>
{
    public async Task<Result<ReceiveStockResponse>> HandleAsync(ReceiveStockCommand command, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == command.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<ReceiveStockResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        // No-op for non-stocked items (e.g. Service) — matches the FE engine's existing behavior.
        if (!item.IsStocked)
            return Result.Success(new ReceiveStockResponse(null));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<ReceiveStockResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        // Idempotency guard — a retry of the same reference (e.g. re-approving a document whose GL
        // posting failed after stock had already moved) must not double-post the same movement. A
        // reversal intentionally shares the same reference as the original post, so it's excluded
        // here — "already posted" means a real, still-standing post exists, not that one ever did.
        var alreadyPostedCost = await dbContext.StockLedgers
            .Where(l => l.ReferenceTable == command.ReferenceTable && l.ReferenceId == command.ReferenceId && !l.IsReversal)
            .Select(l => (decimal?)l.UnitCost)
            .FirstOrDefaultAsync(cancellationToken);
        if (alreadyPostedCost.HasValue)
            return Result.Success(new ReceiveStockResponse(alreadyPostedCost));

        var batchNo = StockBalanceLocker.NormalizeBatch(command.BatchNo);
        var uomCode = await dbContext.Uoms.Where(u => u.Id == item.BaseUomId).Select(u => u.Code).FirstOrDefaultAsync(cancellationToken);

        // The Aspire-configured MySqlRetryingExecutionStrategy refuses to run inside a
        // user-managed BeginTransactionAsync unless the whole unit of work — begin, mutate,
        // commit — is itself the thing being retried; see CreateExecutionStrategy() docs.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs this whole delegate — clear anything the previous, failed attempt
            // left tracked (but never saved) so we start from a clean slate.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var lockedRows = await StockBalanceLocker.LockItemWarehousePairsAsync(dbContext, [(command.ItemId, command.WarehouseId)], cancellationToken);
            var balance = StockBalanceLocker.GetOrCreate(dbContext, lockedRows, command.ItemId, command.BranchId, command.WarehouseId, batchNo);

            // Fifo-costed items get a new CostLayer; every item's StockBalance.AvgUnitCost is kept
            // as a running weighted-average of on-hand value regardless of costing method —
            // informational for Fifo, authoritative for Avg.
            if (item.CostingMethod == "Fifo")
            {
                dbContext.CostLayers.Add(new CostLayer
                {
                    ItemId = command.ItemId,
                    WarehouseId = command.WarehouseId,
                    BatchNo = batchNo,
                    ReceiptDate = command.TransactionDate,
                    SourceReferenceTable = command.ReferenceTable,
                    SourceReferenceId = command.ReferenceId,
                    QtyReceived = command.Qty,
                    QtyRemaining = command.Qty,
                    UnitCost = command.UnitCost
                });
            }

            var newQty = balance.QtyOnHand + command.Qty;
            var newValue = balance.TotalValue + command.Qty * command.UnitCost;
            balance.QtyOnHand = newQty;
            balance.TotalValue = newValue;
            balance.AvgUnitCost = newQty > 0 ? newValue / newQty : 0;
            balance.LastMovementDate = command.TransactionDate;

            dbContext.StockLedgers.Add(new StockLedger
            {
                ItemId = command.ItemId,
                ItemCode = item.Code,
                ItemName = item.Name,
                UomCode = uomCode,
                BranchId = command.BranchId,
                WarehouseId = command.WarehouseId,
                BatchNo = batchNo,
                TransactionType = command.TransactionType ?? "GOODS_RECEIPT",
                ReferenceTable = command.ReferenceTable,
                ReferenceId = command.ReferenceId,
                QtyIn = command.Qty,
                QtyOut = 0,
                UnitCost = command.UnitCost,
                RunningBalanceQty = newQty,
                RunningBalanceValue = newValue,
                IsReversal = false,
                TransactionDate = command.TransactionDate,
                PostedAt = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return Result.Success(new ReceiveStockResponse(command.UnitCost));
    }
}
