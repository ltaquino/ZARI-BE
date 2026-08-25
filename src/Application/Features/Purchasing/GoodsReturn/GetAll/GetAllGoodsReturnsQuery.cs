namespace ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGoodsReturnsQuery : IQuery<Result<List<GoodsReturnResponse>>>;

public sealed record GoodsReturnLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    string? BatchNo,
    string? SerialNo,
    decimal QtyReturned,
    Guid UomId,
    string UomCode,
    decimal UnitCost);

public sealed record GoodsReturnResponse(
    Guid Id,
    string ReturnNo,
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid? GoodsReceiptPoId,
    string ReasonCode,
    DateTimeOffset ReturnDate,
    string Status,
    string? Remarks,
    List<GoodsReturnLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
