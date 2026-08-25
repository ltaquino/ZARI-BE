namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Shared;

using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Entities;

internal static class PurchaseOrderMapper
{
    public static PurchaseOrderResponse ToResponse(PurchaseOrder order) => new(
        order.Id,
        order.PoNo,
        order.BranchId,
        order.SupplierId,
        order.Supplier.Code,
        order.Supplier.Name,
        order.OrderDate,
        order.ExpectedDate,
        order.Status,
        order.Remarks,
        order.Lines.Select(ToLineResponse).ToList(),
        order.CancelledBy,
        order.CancelledAt,
        order.CancelReason,
        order.CreatedAt,
        order.CreatedBy);

    private static PurchaseOrderLineResponse ToLineResponse(PurchaseOrderLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Qty,
        line.UomId,
        line.Uom.Code,
        line.UnitCost);
}
