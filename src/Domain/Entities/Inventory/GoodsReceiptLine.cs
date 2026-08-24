namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsReceiptLine : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public decimal QtyReceived { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }

    // Optional bin — increments StockLocationBalance, not the StockLedger balance itself.
    public Guid? LocationId { get; set; }
    public StorageLocation? Location { get; set; }
}
