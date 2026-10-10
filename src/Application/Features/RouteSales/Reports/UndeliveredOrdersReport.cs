namespace ZARI.Application.Features.RouteSales.Reports.UndeliveredOrders;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;

/// <summary>
/// Undelivered / back-orders (RouteSalesContext.md §6.4): lines of approved sales orders that went out
/// on a trip and are still not fully delivered — what to put on the next trip. TripId narrows to one
/// trip's orders; BranchId to one branch; neither = every branch the caller may view trips for.
/// Orders currently out on an open trip are flagged, not hidden, so the dispatcher sees them too.
/// </summary>
public sealed record GetUndeliveredOrdersReportQuery(string? BranchId, Guid? TripId) : IQuery<Result<UndeliveredOrdersReportResponse>>;

public sealed record UndeliveredOrderLine(
    Guid SalesOrderId,
    string SoNo,
    string BranchId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string CustomerName,
    Guid SalesOrderLineId,
    string ItemCode,
    string ItemName,
    decimal QtyOrdered,
    decimal QtyDelivered,
    decimal QtyOpen,
    decimal OpenAmount,
    string LastTripNo,
    DateTimeOffset LastTripDate,
    string LastTripStatus,
    /// <summary>The order is on a trip that hasn't closed yet — it may still be delivered on it.</summary>
    bool OnOpenTrip,
    /// <summary>Why it wasn't delivered the last time the driver recorded a reason for this line (any trip).</summary>
    string? LastRefusalReason,
    DateTimeOffset? LastRefusedAt);

public sealed record UndeliveredOrdersReportResponse(List<UndeliveredOrderLine> Lines, decimal TotalQtyOpen, decimal TotalOpenAmount);

public sealed class GetUndeliveredOrdersReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetUndeliveredOrdersReportQuery, Result<UndeliveredOrdersReportResponse>>
{
    public async Task<Result<UndeliveredOrdersReportResponse>> HandleAsync(GetUndeliveredOrdersReportQuery query, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        if (query.BranchId is not null && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, query.BranchId, ct))
            return Result.Failure<UndeliveredOrdersReportResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        var assignments = await dbContext.TripSalesOrders.AsNoTracking()
            .Where(s => s.Trip.Status != TripStatus.Cancelled
                && (query.BranchId == null || s.Trip.BranchId == query.BranchId)
                && (query.TripId == null || s.TripId == query.TripId))
            .Select(s => new { s.SalesOrderId, s.Trip.TripNo, s.Trip.TripDate, s.Trip.Status, s.Trip.CreatedAt })
            .ToListAsync(ct);
        if (query.TripId is not null)
        {
            // One trip asked for: still judge "last trip" across every trip the order went out on.
            var ids = assignments.Select(a => a.SalesOrderId).Distinct().ToList();
            assignments = await dbContext.TripSalesOrders.AsNoTracking()
                .Where(s => s.Trip.Status != TripStatus.Cancelled && ids.Contains(s.SalesOrderId))
                .Select(s => new { s.SalesOrderId, s.Trip.TripNo, s.Trip.TripDate, s.Trip.Status, s.Trip.CreatedAt })
                .ToListAsync(ct);
        }
        var lastTrip = assignments.GroupBy(a => a.SalesOrderId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.TripDate).ThenByDescending(a => a.CreatedAt).First());
        var soIds = lastTrip.Keys.ToList();

        var orders = await dbContext.SalesOrders.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.Lines).ThenInclude(l => l.Item)
            .Where(o => soIds.Contains(o.Id) && o.Status == "POSTED")
            .ToListAsync(ct);

        var viewable = new HashSet<string>();
        foreach (var branchId in orders.Select(o => o.BranchId).Distinct())
            if (await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, branchId, ct))
                viewable.Add(branchId);
        orders = orders.Where(o => viewable.Contains(o.BranchId)).ToList();

        var soLineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
        var delivered = await dbContext.DeliveryOrderLines.AsNoTracking()
            .Where(l => l.SalesOrderLineId.HasValue && soLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
            .GroupBy(l => l.SalesOrderLineId!.Value)
            .Select(g => new { Id = g.Key, Qty = g.Sum(l => l.QtyShipped) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, ct);

        var lastRefusal = (await dbContext.TripRefusalLines.AsNoTracking()
                .Where(l => soLineIds.Contains(l.SalesOrderLineId))
                .Select(l => new { l.SalesOrderLineId, l.ReasonCode, l.Notes, l.TripRefusal.RefusedAt })
                .ToListAsync(ct))
            .GroupBy(l => l.SalesOrderLineId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.RefusedAt).First());

        var lines = new List<UndeliveredOrderLine>();
        foreach (var order in orders.OrderBy(o => o.OrderDate).ThenBy(o => o.SoNo))
        {
            var trip = lastTrip[order.Id];
            foreach (var line in order.Lines.OrderBy(l => l.Item.Code))
            {
                var done = delivered.GetValueOrDefault(line.Id);
                var open = line.Qty - done;
                if (open <= 0) continue;
                var netPrice = line.UnitPrice * (1 - line.DiscountPct / 100m) * (1 - (order.DiscountPct ?? 0) / 100m);
                lines.Add(new UndeliveredOrderLine(
                    order.Id, order.SoNo, order.BranchId, order.OrderDate, order.ExpectedDeliveryDate, order.Customer.Name, line.Id,
                    line.Item.Code, line.Item.Name, line.Qty, done, open, Math.Round(open * netPrice, 2),
                    trip.TripNo, trip.TripDate, trip.Status, trip.Status != TripStatus.Closed,
                    lastRefusal.TryGetValue(line.Id, out var r) ? (r.Notes is null ? TripRefusalReason.Label(r.ReasonCode) : $"{TripRefusalReason.Label(r.ReasonCode)} — {r.Notes}") : null,
                    r?.RefusedAt));
            }
        }

        return Result.Success(new UndeliveredOrdersReportResponse(lines, lines.Sum(l => l.QtyOpen), lines.Sum(l => l.OpenAmount)));
    }
}
