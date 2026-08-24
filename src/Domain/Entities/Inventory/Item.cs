namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Item : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public Guid BaseUomId { get; set; }
    public Uom BaseUom { get; set; } = default!;
    public string ItemType { get; set; } = default!;
    public string CostingMethod { get; set; } = default!;
    public bool IsSerialized { get; set; }
    public bool IsBatchTracked { get; set; }
    public bool IsSold { get; set; }
    public bool IsPurchased { get; set; }
    public bool IsStocked { get; set; }

    // GL account references — the Accounting module isn't a backend entity yet,
    // so these stay plain strings (not Guid/FK) until it exists.
    public string? SalesAccountId { get; set; }
    public string? PurchaseAccountId { get; set; }
    public string? InventoryAccountId { get; set; }
    public string? CogsAccountId { get; set; }

    public string Status { get; set; } = default!;
}
