namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class GoodsIssueLine : BaseEntity
{
    public Guid GoodsIssueId { get; set; }
    public GoodsIssue GoodsIssue { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public decimal QtyIssued { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public decimal UnitCost { get; set; }
}
