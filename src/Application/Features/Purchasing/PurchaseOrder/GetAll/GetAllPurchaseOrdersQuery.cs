namespace ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllPurchaseOrdersQuery : IQuery<Result<List<PurchaseOrderResponse>>>;

public sealed record PurchaseOrderLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal Qty,
    Guid UomId,
    string UomCode,
    decimal UnitCost,
    Guid? PurchaseRequestLineId);

public sealed record PurchaseOrderResponse(
    Guid Id,
    string PoNo,
    string BranchId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDate,
    string Status,
    string? Remarks,
    Guid? PurchaseRequestId,
    List<PurchaseOrderLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
