namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReceiptPosQuery : IQuery<Result<List<GoodsReceiptPoResponse>>>;

public sealed record GoodsReceiptPoLineResponse(
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

public sealed record GoodsReceiptPoResponse(
    Guid Id,
    string GrpoNo,
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? SupplierInvoiceNo,
    DateTimeOffset ReceiptDate,
    string Status,
    string? Remarks,
    List<GoodsReceiptPoLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
