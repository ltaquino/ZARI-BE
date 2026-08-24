namespace ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockLocationTransfersQuery : IQuery<Result<List<StockLocationTransferResponse>>>;

// ItemCode/ItemName/ItemDescription and From/To bin labels are joined live from
// Item/StorageLocation at read time — same deliberate simplification as GoodsReceiptLineResponse
// (see its doc comment) instead of the FE mock's frozen-at-create-time snapshot fields.
public sealed record StockLocationTransferLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string? BatchNo,
    string? SerialNo,
    Guid FromLocationId,
    string FromLocationLabel,
    Guid ToLocationId,
    string ToLocationLabel,
    decimal Qty);

public sealed record StockLocationTransferResponse(
    Guid Id,
    string TransferNo,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset TransferDate,
    string Status,
    string? Remarks,
    List<StockLocationTransferLineResponse> Lines,
    string? PostedBy,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
