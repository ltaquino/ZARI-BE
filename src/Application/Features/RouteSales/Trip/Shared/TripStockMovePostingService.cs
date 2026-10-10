namespace ZARI.Application.Features.RouteSales.Trips.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Posts a TripStockMove between warehouses of the trip's own branch — the intra-branch transfer
/// ZARI didn't have (GoodsIssue STOCK_TRANSFER requires a different destination branch).
///
/// Issue side (FromWarehouseId set): one batched IssueStockLinesCommand, ledger reference
/// ("TripStockMove-OUT", line id). Receive side (ToWarehouseId set): one ReceiveStockCommand per
/// line at the issue side's cost (or the line's own UnitCost for an OVERAGE, which has no issue
/// side), reference ("TripStockMove-IN", line id). Different reference tables on purpose: the two
/// engines' idempotency guards match on (ReferenceTable, ReferenceId), so sharing one would make a
/// retried receive mistake the issue's ledger row for its own and silently skip.
///
/// No GL here: both warehouses belong to the same branch and an item's inventory account is per
/// item, not per warehouse, so the move is value-neutral. SHORTAGE/OVERAGE value is journaled by
/// the caller (ApproveTripCloseCommandHandler).
///
/// Both stock engines call ChangeTracker.Clear(), so this never relies on entities loaded before
/// a stock call — it reloads the move by id afterwards.
/// </summary>
internal static class TripStockMovePostingService
{
    public const string IssueReference = "TripStockMove-OUT";
    public const string ReceiveReference = "TripStockMove-IN";

    public static async Task<Result> PostAsync(
        IAppDbContext dbContext,
        ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueHandler,
        ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveHandler,
        Guid moveId,
        string branchId,
        DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        var move = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines).FirstAsync(m => m.Id == moveId, cancellationToken);
        if (move.Status == "POSTED" || move.Lines.Count == 0)
            return await MarkPostedAsync(dbContext, moveId, null, cancellationToken);

        var costs = move.Lines.ToDictionary(l => l.Id.ToString(), l => l.UnitCost);

        if (move.FromWarehouseId is { } from)
        {
            var issue = await issueHandler.HandleAsync(new IssueStockLinesCommand(move.Lines.Select(l => new IssueStockLineItem(
                l.ItemId, branchId, from, null, l.Qty, IssueReference, l.Id.ToString(), date, move.MoveType)).ToList()), cancellationToken);
            if (!issue.IsSuccess)
                return Result.Failure(issue.Error!);
            foreach (var (lineId, cost) in issue.Value!.CostsByReferenceId)
                costs[lineId] = cost;
        }

        if (move.ToWarehouseId is { } to)
        {
            foreach (var line in move.Lines)
            {
                var receive = await receiveHandler.HandleAsync(new ReceiveStockCommand(
                    line.ItemId, branchId, to, null, line.Qty, costs[line.Id.ToString()], ReceiveReference, line.Id.ToString(), date, move.MoveType),
                    cancellationToken);
                if (!receive.IsSuccess)
                    return Result.Failure(receive.Error!);
            }
        }

        return await MarkPostedAsync(dbContext, moveId, costs, cancellationToken);
    }

    private static async Task<Result> MarkPostedAsync(IAppDbContext dbContext, Guid moveId, Dictionary<string, decimal>? costs, CancellationToken ct)
    {
        var move = await dbContext.TripStockMoves.Include(m => m.Lines).FirstAsync(m => m.Id == moveId, ct);
        if (costs is not null)
        {
            foreach (var line in move.Lines)
                line.UnitCost = costs.GetValueOrDefault(line.Id.ToString(), line.UnitCost);
        }
        move.Status = "POSTED";
        move.PostedAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
