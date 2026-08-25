namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class PurchaseRequestLine : BaseEntity
{
    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal QtyRequested { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
    public DateTimeOffset? NeededByDate { get; set; }
}
