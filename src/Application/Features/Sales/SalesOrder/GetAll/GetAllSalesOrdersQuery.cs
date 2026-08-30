namespace ZARI.Application.Features.Sales.SalesOrders.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesOrdersQuery : IQuery<Result<List<SalesOrderResponse>>>;

public sealed record SalesOrderLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal Qty,
    Guid UomId,
    string UomCode,
    decimal UnitPrice,
    decimal DiscountPct,
    string? DiscountSourceType,
    Guid? DiscountSourceId);

public sealed record SalesOrderResponse(
    Guid Id,
    string SoNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string Status,
    string? Remarks,
    decimal? DiscountPct,
    List<SalesOrderLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
