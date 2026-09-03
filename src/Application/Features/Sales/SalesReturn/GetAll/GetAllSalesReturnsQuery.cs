namespace ZARI.Application.Features.Sales.SalesReturns.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesReturnsQuery : IQuery<Result<List<SalesReturnResponse>>>;

public sealed record SalesReturnLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal QtyReturned,
    Guid UomId,
    string UomCode,
    decimal UnitPrice,
    Guid? DeliveryOrderLineId,
    string? SerialNo);

public sealed record SalesReturnResponse(
    Guid Id,
    string ReturnNo,
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    string CustomerName,
    Guid? DeliveryOrderId,
    DateTimeOffset ReturnDate,
    string Status,
    string? Remarks,
    List<SalesReturnLineResponse> Lines,
    Guid? CostCenterId,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
