namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }

    // Set only when the order itself references a PurchaseRequest — which of that request's lines
    // this line is fulfilling. Caps how much can be ordered against that request line (see
    // CreatePurchaseOrderCommandHandler) so the same requested qty can't be over-ordered across
    // multiple purchase orders.
    public Guid? PurchaseRequestLineId { get; set; }
    public PurchaseRequestLine? PurchaseRequestLine { get; set; }
}
