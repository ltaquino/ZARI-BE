namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One FIFO cost layer, consumed oldest-first on issue. Only meaningful for Fifo-costed items —
/// Avg-costed items never get one, since their cost is the running weighted average on StockBalance.
/// </summary>
public sealed class CostLayer : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? BatchNo { get; set; }
    public DateTimeOffset ReceiptDate { get; set; }
    public string SourceReferenceTable { get; set; } = default!;
    public string SourceReferenceId { get; set; } = default!;
    public decimal QtyReceived { get; set; }
    public decimal QtyRemaining { get; set; }
    public decimal UnitCost { get; set; }
}
