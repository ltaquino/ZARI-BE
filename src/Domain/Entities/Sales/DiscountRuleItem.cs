namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// One SKU covered by a Scope="ITEM" DiscountRule — a rule holds as many of these as needed, so
/// one promo spanning several specific items is one rule to create/edit/delete, not N duplicate
/// rules kept in sync by hand. Meaningless (empty) for Scope="CATEGORY"/"ALL" rules.
/// </summary>
public sealed class DiscountRuleItem : BaseEntity
{
    public Guid DiscountRuleId { get; set; }
    public DiscountRule DiscountRule { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
}
