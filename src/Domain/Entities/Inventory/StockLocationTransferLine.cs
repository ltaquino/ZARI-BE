namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockLocationTransferLine : BaseEntity
{
    public Guid StockLocationTransferId { get; set; }
    public StockLocationTransfer StockLocationTransfer { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public Guid FromLocationId { get; set; }
    public StorageLocation FromLocation { get; set; } = default!;
    public Guid ToLocationId { get; set; }
    public StorageLocation ToLocation { get; set; } = default!;
    public decimal Qty { get; set; }
}
