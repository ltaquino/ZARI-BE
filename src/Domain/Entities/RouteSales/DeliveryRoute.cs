namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A named delivery route: the ordered list of customers a truck visits (Stops). Trips are planned
/// against a route; DefaultVehicleId only pre-fills trip planning.
/// </summary>
public sealed class DeliveryRoute : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public Guid? DefaultVehicleId { get; set; }
    public Vehicle? DefaultVehicle { get; set; }

    public string Status { get; set; } = default!;

    public List<DeliveryRouteStop> Stops { get; set; } = [];
}
