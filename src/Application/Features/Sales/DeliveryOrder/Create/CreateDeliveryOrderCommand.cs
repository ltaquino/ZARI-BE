namespace ZARI.Application.Features.Sales.DeliveryOrders.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record DeliveryOrderLineInput(Guid ItemId, decimal QtyShipped, Guid UomId, Guid? SalesOrderLineId);

public sealed record CreateDeliveryOrderCommand(
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTimeOffset DeliveryDate,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<DeliveryOrderLineInput> Lines) : ICommand<Result<DeliveryOrderResponse>>;
