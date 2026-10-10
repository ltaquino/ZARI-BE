namespace ZARI.Application.Features.RouteSales.Reports.DriverShortages;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Driver shortages: every closed trip that came back short (stock at cost + short cash), however it was
/// settled — through payroll, paid in by the driver, or written off (the "report only" mode when payroll
/// deduction is turned off in Company Settings) — with a per-driver summary. From/To filter on the trip
/// date; OutstandingOnly keeps just the driver-pays trips with a balance (the collection list). BranchId
/// omitted = every branch the caller may view trips for.
/// </summary>
public sealed record GetDriverShortagesReportQuery(string? BranchId, bool OutstandingOnly = false, DateOnly? From = null, DateOnly? To = null)
    : IQuery<Result<DriverShortagesReportResponse>>;

public sealed record DriverShortageRow(
    Guid TripId,
    string TripNo,
    string BranchId,
    DateTimeOffset TripDate,
    DateTimeOffset? ClosedAt,
    string DriverUserId,
    string DriverName,
    string VehicleCode,
    /// <summary>PAYROLL, DRIVER_PAYS or WRITE_OFF.</summary>
    string Settlement,
    /// <summary>The shortage found at close, whoever bears it.</summary>
    decimal Shortage,
    decimal Charged,
    decimal WrittenOff,
    decimal Paid,
    decimal Balance);

public sealed record DriverShortageSummary(string DriverUserId, string DriverName, int Trips, decimal Shortage, decimal Charged, decimal WrittenOff,
    decimal Paid, decimal Balance);

public sealed record DriverShortagesReportResponse(
    List<DriverShortageRow> Rows,
    List<DriverShortageSummary> Drivers,
    decimal TotalShortage,
    decimal TotalCharged,
    decimal TotalWrittenOff,
    decimal TotalPaid,
    decimal TotalBalance);

public sealed class GetDriverShortagesReportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetDriverShortagesReportQuery, Result<DriverShortagesReportResponse>>
{
    public async Task<Result<DriverShortagesReportResponse>> HandleAsync(GetDriverShortagesReportQuery query, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        if (query.From is { } f && query.To is { } t && t < f)
            return Result.Failure<DriverShortagesReportResponse>(Error.Validation("DriverShortages.InvalidRange", "The end date must not be before the start date."));
        if (query.BranchId is not null && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, query.BranchId, ct))
            return Result.Failure<DriverShortagesReportResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        var trips = await dbContext.Trips.AsNoTracking().Include(x => x.Vehicle)
            .Where(x => x.Status == TripStatus.Closed && (x.ShortageAmount > 0 || x.ShortageCharged > 0)
                && (query.BranchId == null || x.BranchId == query.BranchId)
                && (!query.OutstandingOnly || (x.ShortageSettlement == TripShortageSettlement.DriverPays && x.ShortagePaid < x.ShortageCharged)))
            .ToListAsync(ct);
        // Same DateOnly rule as GET /api/trips?tripDate=.
        trips = trips.Where(x => DateOnly.FromDateTime(x.TripDate.Date) is var d
            && (query.From is null || d >= query.From) && (query.To is null || d <= query.To)).ToList();

        var viewable = new HashSet<string>();
        foreach (var branchId in trips.Select(x => x.BranchId).Distinct())
            if (await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, branchId, ct))
                viewable.Add(branchId);
        trips = trips.Where(x => viewable.Contains(x.BranchId)).ToList();

        var names = await TripReportShared.UserNamesAsync(userManager, trips.Select(x => x.DriverUserId), ct);
        var rows = trips
            .Select(x =>
            {
                // Trips closed before ShortageAmount existed only carry what was charged.
                var shortage = Math.Max(x.ShortageAmount, x.ShortageCharged);
                return new DriverShortageRow(x.Id, x.TripNo, x.BranchId, x.TripDate, x.ClosedAt, x.DriverUserId,
                    names.GetValueOrDefault(x.DriverUserId, x.DriverUserId), x.Vehicle.Code, x.ShortageSettlement ?? TripShortageSettlement.WriteOff,
                    shortage, x.ShortageCharged, shortage - x.ShortageCharged, x.ShortagePaid, TripShared.ShortageBalance(x));
            })
            .OrderBy(r => r.DriverName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.TripDate)
            .ToList();

        var drivers = rows.GroupBy(r => r.DriverUserId)
            .Select(g => new DriverShortageSummary(g.Key, g.First().DriverName, g.Count(), g.Sum(r => r.Shortage), g.Sum(r => r.Charged),
                g.Sum(r => r.WrittenOff), g.Sum(r => r.Paid), g.Sum(r => r.Balance)))
            .OrderByDescending(d => d.Shortage).ToList();

        return Result.Success(new DriverShortagesReportResponse(rows, drivers, rows.Sum(r => r.Shortage), rows.Sum(r => r.Charged),
            rows.Sum(r => r.WrittenOff), rows.Sum(r => r.Paid), rows.Sum(r => r.Balance)));
    }
}
