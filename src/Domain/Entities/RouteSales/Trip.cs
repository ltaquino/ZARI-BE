namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One run of a truck on a route (RouteSalesContext.md §4) — the document that ties together what
/// was ordered, loaded, delivered, sold off the truck, returned and brought back, so every quantity
/// on the Trip Summary report is a posted document's own total.
///
/// DRAFT → (submit) PENDING_LOAD_APPROVAL → (manager approves; LOAD_OUT posts Source → Truck) LOADED
/// → (driver accepts the load on the handheld) IN_TRANSIT → (driver ends: remaining count + cash)
/// PENDING_CLOSE_APPROVAL → (manager approves; LOAD_IN / DAMAGED / variance post) CLOSED.
/// DRAFT or PENDING_LOAD_APPROVAL can be CANCELLED; nothing has moved yet then.
/// </summary>
public sealed class Trip : AuditableEntity
{
    public string TripNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    public Guid? RouteId { get; set; }
    public DeliveryRoute? Route { get; set; }

    /// <summary>The warehouse the truck loads from and unloads good stock back into.</summary>
    public Guid SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = default!;

    /// <summary>The ZARI user signed in on the handheld — only they can record this trip's sales.</summary>
    public string DriverUserId { get; set; } = default!;

    /// <summary>HR employee charged for shortages at close (EmployeeDeduction). Optional.</summary>
    public Guid? DriverEmployeeId { get; set; }
    public Employee? DriverEmployee { get; set; }

    public string? HelperNames { get; set; }
    public DateTimeOffset TripDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public DateTimeOffset? LoadAcceptedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Cash tendered on this trip's sales and collections (maintained as they post).</summary>
    public decimal CashExpected { get; set; }
    public decimal? CashRemitted { get; set; }

    /// <summary>Close-step progress marker, so a retried close never charges the driver twice.</summary>
    public Guid? EmployeeDeductionId { get; set; }

    /// <summary>
    /// How the driver's shortage (stock at cost + short cash) is settled, chosen at close: PAYROLL (one-payroll
    /// EmployeeDeduction — needs the driver linked to an HR employee), DRIVER_PAYS (the driver pays it in;
    /// recorded as TripShortagePayments) or WRITE_OFF (the cooperative absorbs it). Null when there was none.
    /// </summary>
    public string? ShortageSettlement { get; set; }

    /// <summary>The shortage found at close (stock at cost + short cash), whoever bears it — reported even when written off.</summary>
    public decimal ShortageAmount { get; set; }
    public decimal ShortageCharged { get; set; }

    /// <summary>What the driver has paid in so far against a DRIVER_PAYS shortage.</summary>
    public decimal ShortagePaid { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<TripLine> Lines { get; set; } = [];
    public List<TripSalesOrder> SalesOrders { get; set; } = [];
}

/// <summary>
/// One item on a trip. Planned quantities (Ordered, Extra) are set at planning; every other column
/// is maintained by the posting handlers, never typed in:
/// Variance = Loaded + CustomerReturns − Delivered − VanSold − LoadedBack − DamagedOut, settled at close.
/// </summary>
public sealed class TripLine : BaseEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public Guid UomId { get; set; }

    public decimal QtyOrdered { get; set; }
    public decimal QtyExtra { get; set; }
    public decimal QtyLoaded { get; set; }
    public decimal? QtyLoadCheck { get; set; }
    public decimal QtyDelivered { get; set; }
    public decimal QtyVanSold { get; set; }

    /// <summary>Ordered quantity the customer refused or didn't receive (with a reason) — it stays on the truck.</summary>
    public decimal QtyRefused { get; set; }
    public decimal QtyReturnedGood { get; set; }
    public decimal QtyReturnedDamaged { get; set; }

    /// <summary>The driver's count of good stock left on the truck at trip end.</summary>
    public decimal? QtyCountedAtEnd { get; set; }
    public decimal QtyLoadedBack { get; set; }
    public decimal QtyDamagedOut { get; set; }
    public decimal QtyVariance { get; set; }

    public decimal QtyToLoad => QtyOrdered + QtyExtra;
}

public sealed class TripSalesOrder : BaseEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = default!;
}
