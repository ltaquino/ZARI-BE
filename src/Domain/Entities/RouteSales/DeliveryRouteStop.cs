namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>One customer on a Route, in visiting order (Sequence, 1-based).</summary>
public sealed class DeliveryRouteStop : BaseEntity
{
    public Guid RouteId { get; set; }
    public DeliveryRoute Route { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public int Sequence { get; set; }
    public string? Notes { get; set; }
}
