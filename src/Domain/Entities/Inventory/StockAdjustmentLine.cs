namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockAdjustmentLine : BaseEntity
{
    public Guid StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }

    // System qty on hand at the moment this line was added — a snapshot, not re-read at posting time.
    public decimal QtyBefore { get; set; }

    // The corrected/actual qty the encoder is asserting.
    public decimal QtyAfter { get; set; }

    // QtyAfter - QtyBefore, computed server-side rather than trusted from the client (it drives
    // which stock-posting engine each line goes through). Positive increases stock (like a
    // receipt); negative decreases it (like an issue).
    public decimal VarianceQty { get; set; }

    // Cost used to value the variance — auto-filled from the branch's current cost, same
    // treatment as a Goods Issue line.
    public decimal UnitCost { get; set; }
}
