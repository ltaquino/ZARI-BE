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

    public static AdjustmentReason AdjustmentReason(string code = "DAMAGE", string? description = "Damaged in warehouse", string? glAccountId = null, string status = "active") => new()
    {
        Code = code,
        Description = description,
        GlAccountId = glAccountId,
        Status = status
    };

    public static ItemBranchSetting ItemBranchSetting(Guid itemId, string branchId, Guid? defaultWarehouseId = null, string status = "active") => new()
    {
        ItemId = itemId,
        BranchId = branchId,
        DefaultWarehouseId = defaultWarehouseId,
        ReorderPoint = 10,
        MinStock = 5,
        MaxStock = 100,
        Status = status
    };

    public static ItemCategory ItemCategory(string code = "CAT1", string name = "Test Category", Guid? parentCategoryId = null) => new()
    {
        Code = code,
        Name = name,
        ParentCategoryId = parentCategoryId
    };

    public static StorageLocation StorageLocation(Guid warehouseId, string? zone = "A", string? aisle = "1", string? rack = "1", string? binCode = "01") => new()
    {
        WarehouseId = warehouseId,
        Zone = zone,
        Aisle = aisle,
        Rack = rack,
        BinCode = binCode
    };

    public static GoodsReceipt GoodsReceipt(string branchId, Guid warehouseId, Guid itemId, Guid uomId, string status = "DRAFT", string receiptType = "MANUAL", string? reasonCode = "DAMAGE", decimal qty = 10, decimal unitCost = 50, Guid? locationId = null) => new()
    {
        GrNo = $"GR-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        ReceiptType = receiptType,
        GrDate = DateTimeOffset.UtcNow,
        Status = status,
        ReasonCode = receiptType == "MANUAL" ? reasonCode : null,
        Lines = [new GoodsReceiptLine { ItemId = itemId, QtyReceived = qty, UomId = uomId, UnitCost = unitCost, LocationId = locationId }]
    };

    public static GoodsIssue GoodsIssue(string branchId, Guid warehouseId, Guid itemId, Guid uomId, string status = "DRAFT", string referenceType = "INTERNAL_USE", string? reasonCode = "DAMAGE", string? destBranchId = null, Guid? destWarehouseId = null, decimal qty = 10, decimal unitCost = 50) => new()
    {
        GiNo = $"GI-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        ReferenceType = referenceType,
        DestBranchId = referenceType == "STOCK_TRANSFER" ? destBranchId : null,
        DestWarehouseId = referenceType == "STOCK_TRANSFER" ? destWarehouseId : null,
        ReasonCode = referenceType == "STOCK_TRANSFER" ? null : reasonCode,
        GiDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new GoodsIssueLine { ItemId = itemId, QtyIssued = qty, UomId = uomId, UnitCost = unitCost }]
    };

    public static StockAdjustment StockAdjustment(string branchId, Guid warehouseId, Guid itemId, string status = "DRAFT", string? reasonCode = "DAMAGE", decimal qtyBefore = 10, decimal qtyAfter = 15, decimal unitCost = 50) => new()
    {
        AdjustmentNo = $"ADJ-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        AdjustmentDate = DateTimeOffset.UtcNow,
        ReasonCode = reasonCode,
        Status = status,
        Lines = [new StockAdjustmentLine { ItemId = itemId, QtyBefore = qtyBefore, QtyAfter = qtyAfter, VarianceQty = qtyAfter - qtyBefore, UnitCost = unitCost }]
    };

    public static StockOpname StockOpname(string branchId, Guid warehouseId, Guid itemId, string status = "DRAFT", decimal systemQty = 10, decimal countedQty = 8, decimal unitCost = 50) => new()
    {
        OpnameNo = $"OPN-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        CountDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new StockOpnameLine { ItemId = itemId, SystemQty = systemQty, CountedQty = countedQty, VarianceQty = countedQty - systemQty, UnitCost = unitCost }]
    };

    public static StockLocationTransfer StockLocationTransfer(string branchId, Guid warehouseId, Guid itemId, Guid fromLocationId, Guid toLocationId, string status = "DRAFT", decimal qty = 5) => new()
    {
        TransferNo = $"SLT-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        TransferDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new StockLocationTransferLine { ItemId = itemId, FromLocationId = fromLocationId, ToLocationId = toLocationId, Qty = qty }]
    };

    public static StockTransferRequest StockTransferRequest(string sourceBranchId, Guid sourceWarehouseId, string destBranchId, Guid destWarehouseId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10) => new()
    {
        RequestNo = $"STR-{Guid.NewGuid():N}",
        SourceBranchId = sourceBranchId,
        SourceWarehouseId = sourceWarehouseId,
        DestBranchId = destBranchId,
        DestWarehouseId = destWarehouseId,
        RequestDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new StockTransferRequestLine { ItemId = itemId, QtyRequested = qty, UomId = uomId }]
    };
}
