namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// The physical fulfillment step — decrements stock and books COGS directly on Approve (no
/// clearing/accrual account; see SalesModuleContext.md's Delivery-&gt;Invoice GL timing decision).
/// Sales Invoice references this, never the Sales Order directly, for its own qty-tracking chain.
/// </summary>
public sealed class DeliveryOrder : AuditableEntity
{
    public string DoNo { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    // Optional — a Delivery doesn't have to trace back to a Sales Order on file (e.g. a walk-in
    // over-the-counter sale with no prior order).
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public DateTimeOffset DeliveryDate { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
    public List<DeliveryOrderLine> Lines { get; set; } = [];

    // Optional departmental tag, applied to every line of this document's posted GL journal.
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
