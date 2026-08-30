namespace ZARI.Application.Features.Sales.SalesReturns.Shared;

using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Entities;

internal static class SalesReturnMapper
{
    public static SalesReturnResponse ToResponse(SalesReturn salesReturn) => new(
        salesReturn.Id,
        salesReturn.ReturnNo,
        salesReturn.BranchId,
        salesReturn.WarehouseId,
        salesReturn.CustomerId,
        salesReturn.Customer.Name,
        salesReturn.DeliveryOrderId,
        salesReturn.ReturnDate,
        salesReturn.Status,
        salesReturn.Remarks,
        salesReturn.Lines.Select(ToLineResponse).ToList(),
        salesReturn.CostCenterId,
        salesReturn.CancelledBy,
        salesReturn.CancelledAt,
        salesReturn.CancelReason,
        salesReturn.CreatedAt,
        salesReturn.CreatedBy);

    private static SalesReturnLineResponse ToLineResponse(SalesReturnLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.QtyReturned,
        line.UomId,
        line.Uom.Code,
        line.UnitPrice,
        line.DeliveryOrderLineId);
}
