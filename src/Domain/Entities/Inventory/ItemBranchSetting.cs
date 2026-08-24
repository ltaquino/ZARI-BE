namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>Per-item, per-branch reorder settings — drives the low-stock signal on Stock Balances.</summary>
public sealed class ItemBranchSetting : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    // References the Branch mock/system-module data, which isn't a backend
    // entity yet — kept as a plain string (not Guid/FK) until that module exists.
    public string BranchId { get; set; } = default!;

    public Guid? DefaultWarehouseId { get; set; }
    public Warehouse? DefaultWarehouse { get; set; }

    public decimal ReorderPoint { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public string Status { get; set; } = default!;
}
