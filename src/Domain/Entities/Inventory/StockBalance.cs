namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// Denormalized current on-hand snapshot for (Item, Warehouse, BatchNo), maintained exclusively by
/// the stock-ledger posting handlers (Receive/Issue/Reverse) under an explicit row/gap lock — never
/// written to directly by anything else.
/// </summary>
public sealed class StockBalance : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? BatchNo { get; set; }
    public decimal QtyOnHand { get; set; }
    public decimal AvgUnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTimeOffset? LastMovementDate { get; set; }
}
