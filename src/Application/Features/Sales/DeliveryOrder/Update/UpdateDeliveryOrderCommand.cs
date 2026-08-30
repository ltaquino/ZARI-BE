namespace ZARI.Application.Features.Sales.DeliveryOrders.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.Create;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateDeliveryOrderCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTimeOffset DeliveryDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<DeliveryOrderLineInput> Lines) : ICommand<Result<DeliveryOrderResponse>>;
