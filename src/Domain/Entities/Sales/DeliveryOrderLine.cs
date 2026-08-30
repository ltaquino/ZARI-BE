namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class DeliveryOrderLine : BaseEntity
{
    public Guid DeliveryOrderId { get; set; }
    public DeliveryOrder DeliveryOrder { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal QtyShipped { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;

    // Snapshotted from StockBalance.AvgUnitCost at Approve time (via IssueStockLinesCommand's own
    // costing) — this is what COGS actually books at, independent of the sale's UnitPrice.
    public decimal UnitCost { get; set; }

    // Set only when the delivery itself references a SalesOrder — which of that order's lines this
    // line is fulfilling. Caps how much can be delivered against that order line (Phase 18 pattern)
    // so the same ordered qty can't be over-delivered across multiple deliveries.
    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }
}
