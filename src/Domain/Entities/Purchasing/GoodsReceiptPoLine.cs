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
}
