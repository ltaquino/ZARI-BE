namespace ZARI.Application.Features.Sales.DeliveryOrders.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllDeliveryOrdersQuery : IQuery<Result<List<DeliveryOrderResponse>>>;

public sealed record DeliveryOrderLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal QtyShipped,
    Guid UomId,
    string UomCode,
    decimal UnitCost,
    Guid? SalesOrderLineId);

public sealed record DeliveryOrderResponse(
    Guid Id,
    string DoNo,
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    string CustomerName,
    Guid? SalesOrderId,
    DateTimeOffset DeliveryDate,
    string Status,
    string? Remarks,
    Guid? CostCenterId,
    List<DeliveryOrderLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
