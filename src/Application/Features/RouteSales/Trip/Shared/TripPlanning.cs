namespace ZARI.Application.Features.RouteSales.Trips.Shared;

using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Extra stock loaded for selling off the truck (van sales), on top of the orders.</summary>
public sealed record TripExtraLineInput(Guid ItemId, decimal Qty);

/// <summary>The planning fields shared by Create and Update.</summary>
public interface ITripPlan
{
    Guid VehicleId { get; }
    Guid? RouteId { get; }
    Guid SourceWarehouseId { get; }
    string DriverUserId { get; }
    Guid? DriverEmployeeId { get; }
    string? HelperNames { get; }
    DateTimeOffset TripDate { get; }
    string? Remarks { get; }
    List<Guid> SalesOrderIds { get; }
    List<TripExtraLineInput> ExtraLines { get; }
}

internal static class TripPlanning
{
    public static void ApplyRules<T>(AbstractValidator<T> v) where T : ITripPlan
    {
        v.RuleFor(x => x.VehicleId).NotEmpty();
        v.RuleFor(x => x.SourceWarehouseId).NotEmpty();
        v.RuleFor(x => x.DriverUserId).NotEmpty();
        v.RuleFor(x => x.TripDate).NotEmpty();
        v.RuleFor(x => x.HelperNames).MaximumLength(300);
        v.RuleFor(x => x.Remarks).MaximumLength(300);
        v.RuleFor(x => x.SalesOrderIds).NotNull();
        v.RuleFor(x => x.ExtraLines).NotNull();
        v.RuleFor(x => x).Must(x => (x.SalesOrderIds?.Count ?? 0) + (x.ExtraLines?.Count ?? 0) > 0)
            .WithName("Lines").WithMessage("Add at least one sales order or extra item to load.");
        v.RuleForEach(x => x.ExtraLines).ChildRules(l =>
        {
            l.RuleFor(e => e.ItemId).NotEmpty();
            l.RuleFor(e => e.Qty).GreaterThan(0);
        });
        v.RuleFor(x => x.ExtraLines).Must(l => l == null || l.Select(e => e.ItemId).Distinct().Count() == l.Count)
            .WithMessage("List each extra item once.");
    }

    public sealed record Plan(Guid? DriverEmployeeId, List<TripLine> Lines, List<TripSalesOrder> SalesOrders);

    /// <summary>
    /// Validates the header references and turns orders + extras into trip lines: QtyOrdered is each
    /// assigned sales order's still-undelivered quantity per item, QtyExtra the van-sale stock.
    /// </summary>
    public static async Task<Result<Plan>> BuildAsync<T>(IAppDbContext dbContext, UserManager<ApplicationUser> userManager,
        string branchId, T plan, Guid? excludeTripId, CancellationToken ct) where T : ITripPlan
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == plan.VehicleId, ct);
        if (vehicle is null || vehicle.BranchId != branchId)
            return Fail("Trip.VehicleNotFound", "The vehicle was not found at this branch.");
        if (vehicle.Status != "active")
            return Fail("Trip.VehicleInactive", "This vehicle is inactive.");

        var source = await dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == plan.SourceWarehouseId, ct);
        if (source is null || source.BranchId != branchId)
            return Fail("Trip.SourceWarehouseNotFound", "The source warehouse was not found at this branch.");
        if (source.WarehouseType is "Vehicle" or "Damaged")
            return Fail("Trip.InvalidSourceWarehouse", "A trip loads from a regular warehouse, not a truck or the Damaged warehouse.");

        if (plan.RouteId is { } routeId && !await dbContext.DeliveryRoutes.AnyAsync(r => r.Id == routeId && r.BranchId == branchId, ct))
            return Fail("Trip.RouteNotFound", "The route was not found at this branch.");

        if (await userManager.FindByIdAsync(plan.DriverUserId) is null)
            return Fail("Trip.DriverNotFound", "The driver's user account was not found.");

        // The HR record shortages are charged to: as given, else the employee linked to the driver's login.
        var employeeId = plan.DriverEmployeeId;
        if (employeeId is { } eid)
        {
            if (!await dbContext.Employees.AnyAsync(e => e.Id == eid, ct))
                return Fail("Trip.DriverEmployeeNotFound", "The driver's employee record was not found.");
        }
        else
        {
            employeeId = await dbContext.Employees.Where(e => e.ApplicationUserId == plan.DriverUserId)
                .Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
        }

        var soIds = plan.SalesOrderIds.Distinct().ToList();
        var orders = await dbContext.SalesOrders.AsNoTracking().Include(o => o.Lines)
            .Where(o => soIds.Contains(o.Id)).ToListAsync(ct);
        if (orders.Count != soIds.Count)
            return Fail("Trip.SalesOrderNotFound", "One or more sales orders were not found.");
        if (orders.Any(o => o.BranchId != branchId))
            return Fail("Trip.SalesOrderOtherBranch", "Every sales order must belong to the trip's branch.");
        if (orders.FirstOrDefault(o => o.Status != "POSTED") is { } unapproved)
            return Fail("Trip.SalesOrderNotApproved", $"Sales order {unapproved.SoNo} must be approved before it can be loaded.");

        var onOtherTrip = await dbContext.TripSalesOrders
            .Where(s => soIds.Contains(s.SalesOrderId) && s.TripId != excludeTripId
                && s.Trip.Status != TripStatus.Closed && s.Trip.Status != TripStatus.Cancelled)
            .Select(s => s.SalesOrder.SoNo).FirstOrDefaultAsync(ct);
        if (onOtherTrip is not null)
            return Fail("Trip.SalesOrderOnAnotherTrip", $"Sales order {onOtherTrip} is already on another open trip.");

        var soLineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
        var delivered = await dbContext.DeliveryOrderLines
            .Where(l => l.SalesOrderLineId.HasValue && soLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
            .GroupBy(l => l.SalesOrderLineId!.Value)
            .Select(g => new { Id = g.Key, Qty = g.Sum(l => l.QtyShipped) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, ct);

        var ordered = new Dictionary<Guid, decimal>();
        foreach (var soLine in orders.SelectMany(o => o.Lines))
        {
            var remaining = soLine.Qty - delivered.GetValueOrDefault(soLine.Id);
            if (remaining > 0) ordered[soLine.ItemId] = ordered.GetValueOrDefault(soLine.ItemId) + remaining;
        }
        if (soIds.Count > 0 && ordered.Count == 0 && plan.ExtraLines.Count == 0)
            return Fail("Trip.NothingToDeliver", "These sales orders have already been fully delivered.");

        var itemIds = ordered.Keys.Concat(plan.ExtraLines.Select(e => e.ItemId)).Distinct().ToList();
        var items = await dbContext.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        if (items.Count != itemIds.Count)
            return Fail("Trip.ItemNotFound", "One or more items were not found.");
        if (items.Values.FirstOrDefault(i => !i.IsStocked) is { } service)
            return Fail("Trip.ItemNotStocked", $"{service.Code} is not a stocked item and can't be loaded on a truck.");

        var extras = plan.ExtraLines.ToDictionary(e => e.ItemId, e => e.Qty);
        var lines = itemIds.Select(id => new TripLine
        {
            ItemId = id,
            UomId = items[id].BaseUomId,
            QtyOrdered = ordered.GetValueOrDefault(id),
            QtyExtra = extras.GetValueOrDefault(id),
        }).ToList();

        return Result.Success(new Plan(employeeId, lines, soIds.Select(id => new TripSalesOrder { SalesOrderId = id }).ToList()));
    }

    private static Result<Plan> Fail(string code, string message) => Result.Failure<Plan>(Error.Validation(code, message));
}
