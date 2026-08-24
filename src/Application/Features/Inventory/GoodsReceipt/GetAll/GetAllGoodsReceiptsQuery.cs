namespace ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReceiptsQuery : IQuery<Result<List<GoodsReceiptResponse>>>;

// ItemCode/ItemName/ItemDescription/UomCode are joined live from Item/Uom at read time — unlike the
// FE mock's frozen-at-create-time snapshot, since Item/Uom are now real backend entities and a live
// join is simpler and strictly more accurate than duplicating their fields onto every line.
public sealed record GoodsReceiptLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string? BatchNo,
    string? SerialNo,
    decimal QtyReceived,
    Guid UomId,
    string UomCode,
    decimal UnitCost,
    Guid? LocationId);

public sealed record GoodsReceiptResponse(
    Guid Id,
    string GrNo,
    string BranchId,
    Guid WarehouseId,
    string ReceiptType,
    string? ReceivedBy,
    DateTimeOffset GrDate,
    string Status,
    string? Remarks,
    List<GoodsReceiptLineResponse> Lines,
    string? GoodsIssueRefNo,
    string? GoodsIssueId,
    string? ReasonCode,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
