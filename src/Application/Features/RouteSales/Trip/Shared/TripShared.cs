namespace ZARI.Application.Features.RouteSales.Trips.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public static class TripStatus
{
    public const string Draft = "DRAFT";
    public const string PendingLoadApproval = "PENDING_LOAD_APPROVAL";
    public const string Loaded = "LOADED";
    public const string InTransit = "IN_TRANSIT";
    public const string PendingCloseApproval = "PENDING_CLOSE_APPROVAL";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";

    /// <summary>A truck can run several trips a day, but only one at a time is out of the warehouse.
    /// A List, not an array: under C# 14 <c>array.Contains(x)</c> inside an EF query binds to the
    /// span-based MemoryExtensions overload, which EF Core can't translate.</summary>
    public static readonly List<string> OnTheRoad = [Loaded, InTransit, PendingCloseApproval];
}

public static class TripMoveType
{
    public const string LoadOut = "LOAD_OUT";
    public const string LoadIn = "LOAD_IN";
    public const string Damaged = "DAMAGED";
    public const string Shortage = "SHORTAGE";
    public const string Overage = "OVERAGE";
}

/// <summary>Why ordered quantity wasn't delivered at a stop. A fixed list so the handheld can offer it offline
/// and the reports can count by reason; OTHER needs notes.</summary>
public static class TripRefusalReason
{
    public const string Other = "OTHER";

    public static readonly IReadOnlyList<RefusalReasonOption> All =
    [
        new("CUSTOMER_REFUSED", "Customer refused / changed their mind"),
        new("NO_PAYMENT", "Customer can't pay"),
        new("STORE_CLOSED", "Store closed / nobody to receive"),
        new("DAMAGED", "Damaged or spoiled on arrival"),
        new("WRONG_ITEM", "Wrong item or not what was ordered"),
        new("OVERSTOCKED", "Customer has enough stock"),
        new("PRICE_DISPUTE", "Price dispute"),
        new("NOT_ON_TRUCK", "Not on the truck / ran out"),
        new(Other, "Other (explain in notes)"),
    ];

    public static bool IsValid(string? code) => code is not null && All.Any(r => r.Code == code);
    public static string Label(string code) => All.FirstOrDefault(r => r.Code == code)?.Label ?? code;
}

public sealed record RefusalReasonOption(string Code, string Label);

/// <summary>How a trip's shortage is settled at close (Trip.ShortageSettlement).</summary>
public static class TripShortageSettlement
{
    /// <summary>One-payroll EmployeeDeduction — the driver must be linked to an HR employee.</summary>
    public const string Payroll = "PAYROLL";
    /// <summary>The driver pays it in; each payment is a TripShortagePayment.</summary>
    public const string DriverPays = "DRIVER_PAYS";
    /// <summary>The cooperative absorbs it (inventory variance / other expense); it is still reported. The default
    /// when Company.RouteShortagePayrollDeductionEnabled is off ("report only").</summary>
    public const string WriteOff = "WRITE_OFF";
    public static readonly List<string> All = [Payroll, DriverPays, WriteOff];
}

public sealed record TripLineResponse(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    Guid UomId,
    decimal QtyOrdered,
    decimal QtyExtra,
    decimal QtyLoaded,
    decimal? QtyLoadCheck,
    decimal QtyDelivered,
    decimal QtyVanSold,
    decimal QtyRefused,
    decimal QtyReturnedGood,
    decimal QtyReturnedDamaged,
    decimal? QtyCountedAtEnd,
    decimal QtyLoadedBack,
    decimal QtyDamagedOut,
    decimal QtyVariance,
    /// <summary>Good stock that should be on the truck right now, from the trip's own documents.</summary>
    decimal QtyExpectedOnTruck);

public sealed record TripResponse(
    Guid Id,
    string TripNo,
    string BranchId,
    Guid VehicleId,
    string VehicleCode,
    string PlateNo,
    Guid TruckWarehouseId,
    Guid PosTerminalId,
    Guid? RouteId,
    Guid SourceWarehouseId,
    string DriverUserId,
    Guid? DriverEmployeeId,
    string? HelperNames,
    DateTimeOffset TripDate,
    string Status,
    string? Remarks,
    DateTimeOffset? LoadAcceptedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? ClosedAt,
    decimal CashExpected,
    decimal? CashRemitted,
    string? ShortageSettlement,
    decimal ShortageAmount,
    decimal ShortageCharged,
    decimal ShortagePaid,
    /// <summary>Still owed by the driver on a DRIVER_PAYS shortage.</summary>
    decimal ShortageBalance,
    List<Guid> SalesOrderIds,
    List<TripLineResponse> Lines,
    DateTimeOffset CreatedAt);

internal static class TripShared
{
    public static IQueryable<Trip> WithDetails(this IQueryable<Trip> query) =>
        query.Include(t => t.Vehicle).Include(t => t.Lines).ThenInclude(l => l.Item).Include(t => t.SalesOrders);

    public static decimal ExpectedGoodOnTruck(TripLine l) =>
        l.QtyLoaded + l.QtyReturnedGood - l.QtyDelivered - l.QtyVanSold - l.QtyLoadedBack;

    public static TripResponse ToResponse(Trip t) => new(
        t.Id, t.TripNo, t.BranchId, t.VehicleId, t.Vehicle.Code, t.Vehicle.PlateNo, t.Vehicle.WarehouseId, t.Vehicle.PosTerminalId,
        t.RouteId, t.SourceWarehouseId, t.DriverUserId, t.DriverEmployeeId, t.HelperNames, t.TripDate, t.Status, t.Remarks,
        t.LoadAcceptedAt, t.EndedAt, t.ClosedAt, t.CashExpected, t.CashRemitted, t.ShortageSettlement, t.ShortageAmount, t.ShortageCharged, t.ShortagePaid,
        ShortageBalance(t),
        t.SalesOrders.Select(s => s.SalesOrderId).ToList(),
        t.Lines.OrderBy(l => l.Item.Code).Select(l => new TripLineResponse(
            l.ItemId, l.Item.Code, l.Item.Name, l.UomId, l.QtyOrdered, l.QtyExtra, l.QtyLoaded, l.QtyLoadCheck, l.QtyDelivered,
            l.QtyVanSold, l.QtyRefused, l.QtyReturnedGood, l.QtyReturnedDamaged, l.QtyCountedAtEnd, l.QtyLoadedBack, l.QtyDamagedOut,
            l.QtyVariance, ExpectedGoodOnTruck(l))).ToList(),
        t.CreatedAt);

    public static async Task<TripResponse> LoadResponseAsync(IAppDbContext dbContext, Guid tripId, CancellationToken ct) =>
        ToResponse(await dbContext.Trips.AsNoTracking().WithDetails().FirstAsync(t => t.Id == tripId, ct));

    /// <summary>The trip line for an item, created (all zeros) when the item wasn't planned —
    /// a van sale or a customer return of something the truck didn't load.</summary>
    public static TripLine LineFor(Trip trip, Item item)
    {
        var line = trip.Lines.FirstOrDefault(l => l.ItemId == item.Id);
        if (line is not null) return line;
        line = new TripLine { TripId = trip.Id, ItemId = item.Id, UomId = item.BaseUomId };
        trip.Lines.Add(line);
        return line;
    }

    /// <summary>
    /// Tag stamped into a downstream document's Remarks (delivery, payment, sales return) so a
    /// resumed attempt can find the document a crashed attempt created but never recorded.
    /// </summary>
    public static string Tag(string kind, Guid id) => $"[{kind}:{id:N}]";

    public static async Task<Result<Guid>> AccountIdAsync(IAppDbContext dbContext, string code, string label, CancellationToken ct)
    {
        var id = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(ct);
        return id is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(id.Value);
    }

    public static decimal ShortageBalance(Trip t) =>
        t.ShortageSettlement == TripShortageSettlement.DriverPays ? Math.Max(0, t.ShortageCharged - t.ShortagePaid) : 0;

    /// <summary>Quantity already refused on this trip, per sales order line — it no longer counts as open on this trip.</summary>
    public static async Task<Dictionary<Guid, decimal>> RefusedOnTripAsync(IAppDbContext dbContext, Guid tripId, List<Guid> salesOrderLineIds, CancellationToken ct) =>
        await dbContext.TripRefusalLines
            .Where(l => l.TripRefusal.TripId == tripId && salesOrderLineIds.Contains(l.SalesOrderLineId))
            .GroupBy(l => l.SalesOrderLineId).Select(g => new { g.Key, Qty = g.Sum(l => l.Qty) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty, ct);

    /// <summary>Only the trip's own driver records on the road; a branch approver may act for them.</summary>
    public static bool IsDriver(Trip trip, string? userId) => userId is not null && trip.DriverUserId == userId;
}
