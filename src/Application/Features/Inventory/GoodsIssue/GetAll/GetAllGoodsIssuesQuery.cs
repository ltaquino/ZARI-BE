namespace ZARI.Application.Features.Inventory.GoodsIssues.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsIssuesQuery : IQuery<Result<List<GoodsIssueResponse>>>;

// ItemCode/ItemName/ItemDescription/UomCode are joined live from Item/Uom at read time — same
// deliberate simplification as GoodsReceiptLineResponse (see its doc comment).
public sealed record GoodsIssueLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string? BatchNo,
    string? SerialNo,
    decimal QtyIssued,
    Guid UomId,
    string UomCode,
    decimal UnitCost);

public sealed record GoodsIssueResponse(
    Guid Id,
    string GiNo,
    string BranchId,
    Guid WarehouseId,
    string ReferenceType,
    string? DestBranchId,
    Guid? DestWarehouseId,
    string? ReasonCode,
    DateTimeOffset GiDate,
    string Status,
    string? ShipmentStatus,
    string? Remarks,
    List<GoodsIssueLineResponse> Lines,
    string? StockTransferRequestRefNo,
    string? StockTransferRequestId,
    Guid? CostCenterId,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
