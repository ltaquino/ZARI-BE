namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A delivery truck/van for Route Sales (ZARI-FE/frs/route-sales/RouteSalesContext.md). Each vehicle
/// owns exactly one Warehouse (WarehouseType "Vehicle" — its rolling stock, so every existing stock
/// engine, report and Delivery/Sales Return works on the truck unchanged) and exactly one PosTerminal
/// with its own BIR invoice series (HasOwnSeries) — the handheld that issues and prints invoices on
/// the road. Both are created together with the vehicle (CreateVehicleCommandHandler) so the three
/// can never drift apart.
/// </summary>
public sealed class Vehicle : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string PlateNo { get; set; } = default!;
    public string? Description { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid PosTerminalId { get; set; }
    public PosTerminal PosTerminal { get; set; } = default!;

    public string Status { get; set; } = default!;
}
