namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReceiptPoLine : BaseEntity
{
    public Guid GoodsReceiptPoId { get; set; }
    public GoodsReceiptPo GoodsReceiptPo { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public decimal QtyReceived { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }
    public Guid? LocationId { get; set; }
    public StorageLocation? Location { get; set; }

    // Set only when the receipt itself references a PurchaseOrder — which of that order's lines
    // this line is receiving against. Caps how much can be received against that order line (see
    // CreateGoodsReceiptPoCommandHandler) so the same ordered qty can't be over-received across
    // multiple goods receipts.
    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
}
