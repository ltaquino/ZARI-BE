namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }
    public Guid UomId { get; set; }
    public Uom Uom { get; set; } = default!;

    // Defaults from ItemBranchSetting.SellingPrice at Create time (a suggestion, not read live
    // again after that) — editable.
    public decimal UnitPrice { get; set; }

    public decimal DiscountPct { get; set; }

    // Audit trail for the Discount Scheme's suggestion-resolution (see DiscountSchemeContext.md
    // §4.2): which source actually produced this line's discount before/if the encoder overrode
    // it. "Manual" / "CustomerStanding" / a DiscountRule.Id.ToString().
    public string? DiscountSourceType { get; set; }
    public Guid? DiscountSourceId { get; set; }
}
