namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReturnLine : BaseEntity
{
    public Guid GoodsReturnId { get; set; }
    public GoodsReturn GoodsReturn { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public decimal QtyReturned { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }

    // Set only when the return itself references a GoodsReceiptPo — which of that receipt's lines
    // this line is returning against. Caps how much can be returned against that receipt line (see
    // CreateGoodsReturnCommandHandler) so the same received qty can't be over-returned across
    // multiple goods returns.
    public Guid? GoodsReceiptPoLineId { get; set; }
    public GoodsReceiptPoLine? GoodsReceiptPoLine { get; set; }
}
