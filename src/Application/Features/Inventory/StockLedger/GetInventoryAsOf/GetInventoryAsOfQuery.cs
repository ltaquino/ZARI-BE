namespace ZARI.Application.Features.Inventory.StockLedgers.GetInventoryAsOf;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>The BIR Annual Inventory List — true point-in-time ending balances, not just today's live snapshot. BranchId narrows to one branch; omitted, returns every branch's. IncludeZero:
/// false (default) drops zero-balance (Item, Warehouse, Batch) rows, same as the FE's former
/// client-side post-filter; true returns every row regardless of balance.</summary>
public sealed record GetInventoryAsOfQuery(DateTimeOffset AsOfDate, string? BranchId, bool IncludeZero = false) : IQuery<Result<List<InventoryAsOfLineResponse>>>;

public sealed record InventoryAsOfLineResponse(
    Guid ItemId,
    string? ItemCode,
    string? ItemName,
    string? UomCode,
    string BranchId,
    Guid WarehouseId,
    string WarehouseName,
    string? BatchNo,
    decimal QtyOnHand,
    decimal AvgUnitCost,
    decimal TotalValue);
