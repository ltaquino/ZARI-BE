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

    // Tags this item to appear as a tappable tile on the POS Mode "Menu" grid (cafe/restaurant-style
    // ordering) instead of only being reachable by scanning/typing its code — always a sub-flag of
    // IsSold, enforced in Create/UpdateItemCommandHandler (there's nothing to tile-display if the
    // item isn't sellable in the first place).
    public bool IsTileDisplay { get; set; }

    // Lets this item's stock-out transactions (Goods Issue, negative Adjustment/Opname, Delivery
    // Order, POS checkout, Goods Return-to-supplier — every one funnels through
    // IssueStockLinesCommandHandler) succeed even when on-hand quantity is zero or insufficient,
    // instead of being hard-blocked. StockBalance.QtyOnHand goes negative to reflect the real
    // (over-)issued position; a later Goods Receipt brings it back up as usual. Only meaningful
    // when IsStocked is also true — an unstocked item never reaches that handler in the first place.
    public bool AllowNegativeStock { get; set; }

    // GL account references — the Accounting module isn't a backend entity yet,
    // so these stay plain strings (not Guid/FK) until it exists.
    public string? SalesAccountId { get; set; }
    public string? PurchaseAccountId { get; set; }
    public string? InventoryAccountId { get; set; }
    public string? CogsAccountId { get; set; }

    // Default VAT classification for this item's Sales Invoice lines (BIR VAT breakdown — see
    // ZARI-FE/frs/sales/SalesModuleContext.md §3.6). "VATABLE" / "VAT_EXEMPT" / "ZERO_RATED".
    // A smart default only — always overridable per line, and a statutory discount on a line
    // (see StatutoryDiscountType) always forces VAT_EXEMPT regardless of this default.
    public string VatType { get; set; } = "VATABLE";

    public string Status { get; set; } = default!;
}
