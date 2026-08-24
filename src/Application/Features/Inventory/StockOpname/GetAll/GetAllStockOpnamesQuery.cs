namespace ZARI.Application.Features.Inventory.StockOpnames.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockOpnamesQuery : IQuery<Result<List<StockOpnameResponse>>>;

// ItemCode/ItemName/ItemDescription/UomCode are joined live from Item/Uom at read time — same
// deliberate simplification as GoodsReceiptLineResponse (see its doc comment). UomCode reflects
// the item's base UOM, since a line has no UomId of its own.
public sealed record StockOpnameLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string UomCode,
    string? BatchNo,
    string? SerialNo,
    decimal SystemQty,
    decimal CountedQty,
    decimal VarianceQty,
    decimal UnitCost);

public sealed record StockOpnameResponse(
    Guid Id,
    string OpnameNo,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset CountDate,
    string Status,
    string? Remarks,
    List<StockOpnameLineResponse> Lines,
    string? PostedBy,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
