namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockTransferRequestLine : BaseEntity
{
    public Guid StockTransferRequestId { get; set; }
    public StockTransferRequest StockTransferRequest { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal QtyRequested { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;
}
