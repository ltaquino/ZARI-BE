namespace ZARI.Application.Features.RouteSales.Reports;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Who/what/when of one trip — the heading every per-trip report prints.</summary>
public sealed record TripReportHeader(
    Guid TripId,
    string TripNo,
    string BranchId,
    DateTimeOffset TripDate,
    string Status,
    string VehicleCode,
    string PlateNo,
    string? RouteCode,
    string? RouteName,
    string DriverUserId,
    string DriverName,
    string? HelperNames,
    string SourceWarehouseName,
    DateTimeOffset? LoadAcceptedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? ClosedAt);

internal static class TripReportShared
{
    /// <summary>
    /// The trip with everything the per-trip reports read, after the same access rule as GET /api/trips/{id}:
    /// the trip's own driver, or anyone who may view trips for its branch.
    /// </summary>
    public static async Task<Result<Trip>> LoadTripAsync(IAppDbContext dbContext, IPermissionService permissionService, ICurrentUser currentUser,
        Guid tripId, CancellationToken ct)
    {
        var trip = await dbContext.Trips.AsNoTracking()
            .Include(t => t.Vehicle).Include(t => t.Route).Include(t => t.SourceWarehouse)
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .Include(t => t.SalesOrders)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
            return Result.Failure<Trip>(Error.NotFound("Trip.NotFound", $"Trip with ID '{tripId}' was not found."));

        if (!TripShared.IsDriver(trip, currentUser.UserId)
            && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, trip.BranchId, ct))
            return Result.Failure<Trip>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        return Result.Success(trip);
    }

    public static async Task<Dictionary<string, string>> UserNamesAsync(UserManager<ApplicationUser> userManager, IEnumerable<string> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        var users = await userManager.Users.AsNoTracking().Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName }).ToListAsync(ct);
        return users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim() is { Length: > 0 } name ? name : u.UserName ?? u.Id);
    }

    public static async Task<TripReportHeader> HeaderAsync(UserManager<ApplicationUser> userManager, Trip trip, CancellationToken ct)
    {
        var names = await UserNamesAsync(userManager, [trip.DriverUserId], ct);
        return new TripReportHeader(
            trip.Id, trip.TripNo, trip.BranchId, trip.TripDate, trip.Status, trip.Vehicle.Code, trip.Vehicle.PlateNo,
            trip.Route?.Code, trip.Route?.Name, trip.DriverUserId, names.GetValueOrDefault(trip.DriverUserId, trip.DriverUserId),
            trip.HelperNames, trip.SourceWarehouse.Name, trip.LoadAcceptedAt, trip.EndedAt, trip.ClosedAt);
    }

    /// <summary>
    /// Unit cost per item for valuing trip quantities: the cost the stock engine charged when the item
    /// left the source (LOAD_OUT), else whatever another of the trip's moves carried (an item that only
    /// came onto the truck as a customer return). Zero when nothing has moved yet (a draft trip).
    /// </summary>
    public static Dictionary<Guid, decimal> UnitCosts(IEnumerable<TripStockMove> moves) =>
        moves.OrderBy(m => m.MoveType == TripMoveType.LoadOut ? 0 : 1)
            .SelectMany(m => m.Lines)
            .Where(l => l.UnitCost > 0)
            .GroupBy(l => l.ItemId)
            .ToDictionary(g => g.Key, g => g.First().UnitCost);

    /// <summary>Stock variance settled at close, at the cost it was posted at: overage positive, shortage negative.</summary>
    public static decimal PostedVarianceValue(IEnumerable<TripStockMove> moves, Guid? itemId = null) =>
        moves.Where(m => m.MoveType is TripMoveType.Shortage or TripMoveType.Overage)
            .SelectMany(m => m.Lines.Where(l => itemId == null || l.ItemId == itemId)
                .Select(l => (m.MoveType == TripMoveType.Overage ? 1 : -1) * l.Qty * l.UnitCost))
            .Sum();

    /// <summary>Remitted − expected (negative = short), once the driver has handed the cash in.</summary>
    public static decimal? CashShortOver(Trip trip) => trip.CashRemitted is { } remitted ? remitted - trip.CashExpected : null;
}
