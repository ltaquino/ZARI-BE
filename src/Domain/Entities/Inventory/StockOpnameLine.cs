namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class StockOpnameLine : BaseEntity
{
    public Guid StockOpnameId { get; set; }
    public StockOpname StockOpname { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }

    // System qty on hand at the moment this line was added — the count sheet's starting point.
    public decimal SystemQty { get; set; }

    // What the physical count actually found.
    public decimal CountedQty { get; set; }

    // CountedQty - SystemQty, computed server-side rather than trusted from the client — same
    // reasoning as StockAdjustmentLine.VarianceQty.
    public decimal VarianceQty { get; set; }

    // Cost used to value the variance, only meaningful when VarianceQty != 0.
    public decimal UnitCost { get; set; }
}
