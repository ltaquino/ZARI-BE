namespace ZARI.Application.Features.Sales.SalesOrders.Shared;

using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Entities;

internal static class SalesOrderMapper
{
    public static SalesOrderResponse ToResponse(SalesOrder order) => new(
        order.Id,
        order.SoNo,
        order.BranchId,
        order.CustomerId,
        order.Customer.Name,
        order.OrderDate,
        order.ExpectedDeliveryDate,
        order.Status,
        order.Remarks,
        order.DiscountPct,
        order.Lines.Select(ToLineResponse).ToList(),
        order.CancelledBy,
        order.CancelledAt,
        order.CancelReason,
        order.CreatedAt,
        order.CreatedBy);

    private static SalesOrderLineResponse ToLineResponse(SalesOrderLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Qty,
        line.UomId,
        line.Uom.Code,
        line.UnitPrice,
        line.DiscountPct,
        line.DiscountSourceType,
        line.DiscountSourceId);
}
