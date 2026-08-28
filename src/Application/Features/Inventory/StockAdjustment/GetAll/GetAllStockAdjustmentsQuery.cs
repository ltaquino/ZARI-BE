namespace ZARI.Application.Features.Inventory.StockAdjustments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockAdjustmentsQuery : IQuery<Result<List<StockAdjustmentResponse>>>;

// ItemCode/ItemName/ItemDescription/UomCode are joined live from Item/Uom at read time — same
// deliberate simplification as GoodsReceiptLineResponse (see its doc comment). UomCode reflects
// the item's base UOM, since a line has no UomId of its own.
public sealed record StockAdjustmentLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string UomCode,
    string? BatchNo,
    string? SerialNo,
    decimal QtyBefore,
    decimal QtyAfter,
    decimal VarianceQty,
    decimal UnitCost);

public sealed record StockAdjustmentResponse(
    Guid Id,
    string AdjustmentNo,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset AdjustmentDate,
    string? ReasonCode,
    string Status,
    string? Remarks,
    List<StockAdjustmentLineResponse> Lines,
    Guid? CostCenterId,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
