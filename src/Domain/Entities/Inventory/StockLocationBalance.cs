namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Bin-level, quantity-only sub-ledger — denormalized and non-authoritative for valuation/COGS
/// (StockBalance stays the source of truth for that; see its type comment). Sums up to
/// StockBalance for a given (ItemId, WarehouseId, BatchNo) but is never itself consulted for
/// costing. Mirrors the FE prototype's stockLocationBalances.ts engine.
/// </summary>
public sealed class StockLocationBalance : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid LocationId { get; set; }
    public StorageLocation Location { get; set; } = default!;

    public string? BatchNo { get; set; }
    public decimal QtyOnHand { get; set; }
    public DateTimeOffset? LastMovementDate { get; set; }
}
