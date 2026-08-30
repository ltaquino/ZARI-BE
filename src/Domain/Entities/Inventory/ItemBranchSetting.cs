namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>Per-item, per-branch reorder settings — drives the low-stock signal on Stock Balances.</summary>
public sealed class ItemBranchSetting : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public Guid? DefaultWarehouseId { get; set; }
    public Warehouse? DefaultWarehouse { get; set; }

    public decimal ReorderPoint { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }

    // Selling price — Sales' "markup suggests, override wins" mechanic (see
    // ZARI-FE/frs/sales/SalesModuleContext.md §3.5). SellingPrice is the single stored,
    // authoritative value every Sales document actually reads at transaction time; MarkupPct is a
    // convenience that computes a one-time *suggested* SellingPrice off this item's branch-wide
    // AvgUnitCost (from StockBalance) when the FE's "recalculate" action is used — never a live
    // formula that silently recomputes SellingPrice on its own.
    public decimal? SellingPrice { get; set; }
    public decimal? MarkupPct { get; set; }

    public string Status { get; set; } = default!;
}
