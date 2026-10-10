namespace ZARI.Application.Features.RouteSales.Reports.RoutePerformance;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Route Performance (RouteSalesContext.md §6.5): CLOSED trips dated From..To, grouped by route, truck
/// or driver — sales, delivery fill rate (delivered ÷ ordered), returns rate, stock variance and cash
/// short/over. Closed trips only, so every figure is settled. BranchId omitted = every branch the
/// caller may view trips for.
/// </summary>
public sealed record GetRoutePerformanceReportQuery(DateOnly From, DateOnly To, string? BranchId, string? GroupBy)
    : IQuery<Result<RoutePerformanceReportResponse>>;

public static class RoutePerformanceGroupBy
{
    public const string Route = "route";
    public const string Vehicle = "vehicle";
    public const string Driver = "driver";
    public static readonly List<string> All = [Route, Vehicle, Driver];
}

public sealed record RoutePerformanceRow(
    string Key,
    string Label,
    int Trips,
    decimal QtyOrdered,
    decimal QtyDelivered,
    /// <summary>Delivered ÷ ordered, as a percentage; null when nothing was ordered (van sales only).</summary>
    decimal? FillRatePct,
    decimal QtyVanSold,
    /// <summary>Ordered quantity not delivered, with a reason.</summary>
    decimal QtyRefused,
    decimal QtyReturned,
    /// <summary>Customer returns ÷ units sold (delivered + van sold), as a percentage.</summary>
    decimal? ReturnsPct,
    decimal SalesAmount,
    decimal QtyVariance,
    /// <summary>Stock variance at posted cost; negative = shortage.</summary>
    decimal VarianceValue,
    /// <summary>Cash remitted − expected; negative = short.</summary>
    decimal CashShortOver,
    /// <summary>Shortage found at close (stock at cost + short cash), whoever bears it.</summary>
    decimal ShortageAmount,
    decimal ShortageCharged);

public sealed record RoutePerformanceReportResponse(
    DateOnly From, DateOnly To, string GroupBy, List<RoutePerformanceRow> Rows, RoutePerformanceRow Total);

public sealed class GetRoutePerformanceReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetRoutePerformanceReportQuery, Result<RoutePerformanceReportResponse>>
{
    public async Task<Result<RoutePerformanceReportResponse>> HandleAsync(GetRoutePerformanceReportQuery query, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var groupBy = (query.GroupBy ?? RoutePerformanceGroupBy.Route).Trim().ToLowerInvariant();
        if (!RoutePerformanceGroupBy.All.Contains(groupBy))
            return Result.Failure<RoutePerformanceReportResponse>(Error.Validation("RoutePerformance.InvalidGroupBy", "Group by must be route, vehicle or driver."));
        if (query.To < query.From)
            return Result.Failure<RoutePerformanceReportResponse>(Error.Validation("RoutePerformance.InvalidRange", "The end date must not be before the start date."));
        if (query.BranchId is not null && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, query.BranchId, ct))
            return Result.Failure<RoutePerformanceReportResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        // A day's margin either side, then the exact date filter in memory (same DateOnly rule as GET /api/trips?tripDate=).
        var fromAt = new DateTimeOffset(query.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var toAt = new DateTimeOffset(query.To.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(2);
        var trips = await dbContext.Trips.AsNoTracking()
            .Include(t => t.Vehicle).Include(t => t.Route).Include(t => t.Lines)
            .Where(t => t.Status == TripStatus.Closed && t.TripDate >= fromAt && t.TripDate < toAt
                && (query.BranchId == null || t.BranchId == query.BranchId))
            .ToListAsync(ct);
        trips = trips.Where(t => DateOnly.FromDateTime(t.TripDate.Date) is var d && d >= query.From && d <= query.To).ToList();

        var viewable = new HashSet<string>();
        foreach (var branchId in trips.Select(t => t.BranchId).Distinct())
            if (await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, branchId, ct))
                viewable.Add(branchId);
        trips = trips.Where(t => viewable.Contains(t.BranchId)).ToList();

        var tripIds = trips.Select(t => t.Id).ToList();
        var moves = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines)
            .Where(m => tripIds.Contains(m.TripId) && (m.MoveType == TripMoveType.Shortage || m.MoveType == TripMoveType.Overage))
            .ToListAsync(ct);
        var sales = await dbContext.RouteSales.AsNoTracking()
            .Where(r => tripIds.Contains(r.TripId) && r.Status == "COMPLETED")
            .GroupBy(r => r.TripId)
            .Select(g => new { TripId = g.Key, Amount = g.Sum(r => r.InvoiceTotal) })
            .ToDictionaryAsync(x => x.TripId, x => x.Amount, ct);
        var driverNames = groupBy == RoutePerformanceGroupBy.Driver
            ? await TripReportShared.UserNamesAsync(userManager, trips.Select(t => t.DriverUserId), ct)
            : [];

        (string Key, string Label) KeyOf(Trip t) => groupBy switch
        {
            RoutePerformanceGroupBy.Vehicle => (t.VehicleId.ToString(), $"{t.Vehicle.Code} ({t.Vehicle.PlateNo})"),
            RoutePerformanceGroupBy.Driver => (t.DriverUserId, driverNames.GetValueOrDefault(t.DriverUserId, t.DriverUserId)),
            _ => t.Route is null ? ("", "(no route)") : (t.Route.Id.ToString(), $"{t.Route.Code} — {t.Route.Name}"),
        };

        RoutePerformanceRow Row(string key, string label, List<Trip> group)
        {
            var lines = group.SelectMany(t => t.Lines).ToList();
            var ordered = lines.Sum(l => l.QtyOrdered);
            var delivered = lines.Sum(l => l.QtyDelivered);
            var vanSold = lines.Sum(l => l.QtyVanSold);
            var returned = lines.Sum(l => l.QtyReturnedGood + l.QtyReturnedDamaged);
            var ids = group.Select(t => t.Id).ToHashSet();
            return new RoutePerformanceRow(
                key, label, group.Count, ordered, delivered,
                ordered > 0 ? Math.Round(delivered / ordered * 100, 2) : null,
                vanSold, lines.Sum(l => l.QtyRefused), returned,
                delivered + vanSold > 0 ? Math.Round(returned / (delivered + vanSold) * 100, 2) : null,
                Math.Round(group.Sum(t => sales.GetValueOrDefault(t.Id)), 2),
                lines.Sum(l => l.QtyVariance),
                Math.Round(TripReportShared.PostedVarianceValue(moves.Where(m => ids.Contains(m.TripId))), 2),
                group.Sum(t => TripReportShared.CashShortOver(t) ?? 0),
                group.Sum(t => Math.Max(t.ShortageAmount, t.ShortageCharged)),
                group.Sum(t => t.ShortageCharged));
        }

        var rows = trips.GroupBy(KeyOf)
            .Select(g => Row(g.Key.Key, g.Key.Label, g.ToList()))
            .OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success(new RoutePerformanceReportResponse(query.From, query.To, groupBy, rows, Row("", "TOTAL", trips)));
    }
}
