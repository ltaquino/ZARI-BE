namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

/// <summary>
/// Entity builders for Inventory master data (Item/Uom/Warehouse) — shared by Purchasing/Sales/
/// Inventory tests alike, since a purchase/sales line always references a real Item+Uom. Same
/// InMemory-provider caveats apply as documented on LoanTestFixtures.
/// </summary>
internal static class InventoryTestFixtures
{
    public static Uom Uom(string code = "PC", string name = "Piece") => new()
    {
        Code = code,
        Name = name
    };

    public static Item Item(
        Guid baseUomId,
        string code = "ITEM-1",
        string name = "Test Item",
        string itemType = "GOODS",
        string costingMethod = "MOVING_AVERAGE",
        bool isStocked = true,
        bool isPurchased = true,
        bool isSold = true,
        string status = "active") => new()
    {
        Code = code,
        Name = name,
        BaseUomId = baseUomId,
        ItemType = itemType,
        CostingMethod = costingMethod,
        IsSerialized = false,
        IsBatchTracked = false,
        IsSold = isSold,
        IsPurchased = isPurchased,
        IsStocked = isStocked,
        IsTileDisplay = false,
        AllowNegativeStock = false,
        VatType = "VATABLE",
        Status = status
    };

    public static Warehouse Warehouse(string branchId, string code = "WH1", string name = "Main Warehouse", string warehouseType = "main", string status = "active") => new()
    {
        BranchId = branchId,
        Code = code,
        Name = name,
        WarehouseType = warehouseType,
        Status = status
    };
}
