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
}
