namespace ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockTransferRequestsQuery : IQuery<Result<List<StockTransferRequestResponse>>>;

// ItemCode/ItemName/ItemDescription/UomCode are joined live from Item/Uom at read time — same
// deliberate simplification as GoodsReceiptLineResponse (see its doc comment).
public sealed record StockTransferRequestLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal QtyRequested,
    Guid UomId,
    string UomCode);

public sealed record StockTransferRequestResponse(
    Guid Id,
    string RequestNo,
    string SourceBranchId,
    Guid SourceWarehouseId,
    string DestBranchId,
    Guid DestWarehouseId,
    DateTimeOffset RequestDate,
    string Status,
    string? Remarks,
    List<StockTransferRequestLineResponse> Lines,
    string? DeclinedBy,
    DateTimeOffset? DeclinedAt,
    string? DeclineReason,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
