namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Per-unit tracking for a serialized item — a simple status machine (IN_STOCK -> IN_TRANSIT /
/// DISPOSED / SOLD -> IN_STOCK), keyed by (ItemId, SerialNo). SOLD is POS's own disposition
/// (PosStockPostingService), added alongside the original IN_TRANSIT/DISPOSED pair GoodsIssue uses.
/// Mirrors the FE prototype's serialNumbers.ts engine; unlike StockBalance this has no cost/
/// valuation role and no concurrent-contention concern worth a FOR UPDATE lock — a given serial is
/// only ever touched by one document at a time.
/// </summary>
public sealed class SerialNumber : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public string SerialNo { get; set; } = default!;

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string Status { get; set; } = default!;
}
