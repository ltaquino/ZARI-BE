namespace ZARI.Application.Features.Sales.DeliveryOrders.Shared;

using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Entities;

internal static class DeliveryOrderMapper
{
    public static DeliveryOrderResponse ToResponse(DeliveryOrder order) => new(
        order.Id,
        order.DoNo,
        order.BranchId,
        order.WarehouseId,
        order.CustomerId,
        order.Customer.Name,
        order.SalesOrderId,
        order.DeliveryDate,
        order.Status,
        order.Remarks,
        order.CostCenterId,
        order.Lines.Select(ToLineResponse).ToList(),
        order.CancelledBy,
        order.CancelledAt,
        order.CancelReason,
        order.CreatedAt,
        order.CreatedBy);

    private static DeliveryOrderLineResponse ToLineResponse(DeliveryOrderLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.QtyShipped,
        line.UomId,
        line.Uom.Code,
        line.UnitCost,
        line.SalesOrderLineId);
}
