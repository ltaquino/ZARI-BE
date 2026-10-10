namespace ZARI.Application.Features.RouteSales.Trips.Get;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed record GetTripQuery(Guid Id) : IQuery<Result<TripResponse>>;

public sealed class GetTripQueryHandler(IAppDbContext dbContext, IPermissionService permissionService, ICurrentUser currentUser)
    : IQueryHandler<GetTripQuery, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(GetTripQuery query, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.AsNoTracking().WithDetails().FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{query.Id}' was not found."));

        if (!TripShared.IsDriver(trip, currentUser.UserId)
            && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        return Result.Success(TripShared.ToResponse(trip));
    }
}

/// <summary>Trips for a branch (or every branch the caller may view), optionally one status / date.</summary>
public sealed record GetAllTripsQuery(string? BranchId, string? Status, DateOnly? TripDate) : IQuery<Result<List<TripResponse>>>;

public sealed class GetAllTripsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllTripsQuery, Result<List<TripResponse>>>
{
    public async Task<Result<List<TripResponse>>> HandleAsync(GetAllTripsQuery query, CancellationToken cancellationToken = default)
    {
        var trips = await dbContext.Trips.AsNoTracking().WithDetails()
            .Where(t => (query.BranchId == null || t.BranchId == query.BranchId) && (query.Status == null || t.Status == query.Status))
            .OrderByDescending(t => t.TripDate).ThenByDescending(t => t.TripNo)
            .ToListAsync(cancellationToken);

        var results = new List<TripResponse>();
        foreach (var trip in trips)
        {
            if (query.TripDate is { } d && DateOnly.FromDateTime(trip.TripDate.Date) != d) continue;
            if (await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, trip.BranchId, cancellationToken))
                results.Add(TripShared.ToResponse(trip));
        }
        return Result.Success(results);
    }
}

public sealed record AvailableSalesOrderLine(Guid SalesOrderLineId, Guid ItemId, string ItemCode, string ItemName, decimal Qty, decimal QtyRemaining, decimal UnitPrice);

public sealed record AvailableSalesOrder(
    Guid Id,
    string SoNo,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    List<AvailableSalesOrderLine> Lines);

/// <summary>
/// The approved sales orders a trip can carry: POSTED, this branch, still something left to deliver, and not
/// already on another trip that hasn't closed (ExcludeTripId = the trip being edited, so its own orders stay pickable).
/// Same rules TripPlanning enforces when the trip is saved.
/// </summary>
public sealed record GetAvailableSalesOrdersQuery(string BranchId, Guid? ExcludeTripId) : IQuery<Result<List<AvailableSalesOrder>>>;

public sealed class GetAvailableSalesOrdersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAvailableSalesOrdersQuery, Result<List<AvailableSalesOrder>>>
{
    public async Task<Result<List<AvailableSalesOrder>>> HandleAsync(GetAvailableSalesOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, query.BranchId, ct))
            return Result.Failure<List<AvailableSalesOrder>>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        var onOpenTrips = await dbContext.TripSalesOrders
            .Where(s => s.TripId != query.ExcludeTripId && s.Trip.Status != TripStatus.Closed && s.Trip.Status != TripStatus.Cancelled)
            .Select(s => s.SalesOrderId).Distinct().ToListAsync(ct);
        var orders = await dbContext.SalesOrders.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.Lines).ThenInclude(l => l.Item)
            .Where(o => o.BranchId == query.BranchId && o.Status == "POSTED" && !onOpenTrips.Contains(o.Id))
            .OrderBy(o => o.ExpectedDeliveryDate ?? o.OrderDate).ThenBy(o => o.SoNo)
            .ToListAsync(ct);

        var lineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
        var delivered = await dbContext.DeliveryOrderLines
            .Where(l => l.SalesOrderLineId.HasValue && lineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
            .GroupBy(l => l.SalesOrderLineId!.Value).Select(g => new { g.Key, Qty = g.Sum(l => l.QtyShipped) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty, ct);

        var result = orders
            .Select(o => new AvailableSalesOrder(o.Id, o.SoNo, o.CustomerId, o.Customer.Name, o.OrderDate, o.ExpectedDeliveryDate,
                o.Lines.OrderBy(l => l.Item.Code)
                    .Select(l => new AvailableSalesOrderLine(l.Id, l.ItemId, l.Item.Code, l.Item.Name, l.Qty, Math.Max(0, l.Qty - delivered.GetValueOrDefault(l.Id)), l.UnitPrice))
                    .ToList()))
            .Where(o => o.Lines.Any(l => l.QtyRemaining > 0))
            .ToList();
        return Result.Success(result);
    }
}

/// <summary>A user who can be put on a trip as its driver; EmployeeId is set when their login is linked to an
/// HR employee (needed for payroll deduction of shortages).</summary>
public sealed record TripDriverOption(string UserId, string Name, string? Email, Guid? EmployeeId, string? EmployeeNo);

/// <summary>
/// Active users assigned to the branch — the trip's driver picker. Served here (TRIPS View) rather than from
/// /api/users, which needs user-admin rights a dispatcher or branch manager usually doesn't have.
/// </summary>
public sealed record GetTripDriversQuery(string BranchId) : IQuery<Result<List<TripDriverOption>>>;

public sealed class GetTripDriversQueryHandler(IAppDbContext dbContext, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetTripDriversQuery, Result<List<TripDriverOption>>>
{
    public async Task<Result<List<TripDriverOption>>> HandleAsync(GetTripDriversQuery query, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, query.BranchId, ct))
            return Result.Failure<List<TripDriverOption>>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        var userIds = await dbContext.UserBranches.Where(b => b.BranchId == query.BranchId).Select(b => b.UserId).ToListAsync(ct);
        var users = await userManager.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.Status == "active")
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(ct);
        var ids = users.Select(u => (string?)u.Id).ToList();
        var employees = await dbContext.Employees.AsNoTracking()
            .Where(e => e.ApplicationUserId != null && ids.Contains(e.ApplicationUserId))
            .Select(e => new { e.Id, e.EmployeeNo, e.ApplicationUserId })
            .ToListAsync(ct);

        return Result.Success(users
            .Select(u =>
            {
                var employee = employees.FirstOrDefault(e => e.ApplicationUserId == u.Id);
                return new TripDriverOption(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email, employee?.Id, employee?.EmployeeNo);
            })
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }
}
