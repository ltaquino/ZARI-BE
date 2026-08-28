namespace ZARI.Application.Features.Purchasing.GoodsReturns.Shared;

using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Entities;

internal static class GoodsReturnMapper
{
    public static GoodsReturnResponse ToResponse(GoodsReturn goodsReturn) => new(
        goodsReturn.Id,
        goodsReturn.ReturnNo,
        goodsReturn.BranchId,
        goodsReturn.WarehouseId,
        goodsReturn.SupplierId,
        goodsReturn.Supplier.Code,
        goodsReturn.Supplier.Name,
        goodsReturn.GoodsReceiptPoId,
        goodsReturn.ReasonCode,
        goodsReturn.ReturnDate,
        goodsReturn.Status,
        goodsReturn.Remarks,
        goodsReturn.Lines.Select(ToLineResponse).ToList(),
        goodsReturn.CostCenterId,
        goodsReturn.CancelledBy,
        goodsReturn.CancelledAt,
        goodsReturn.CancelReason,
        goodsReturn.CreatedAt,
        goodsReturn.CreatedBy);

    private static GoodsReturnLineResponse ToLineResponse(GoodsReturnLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.BatchNo,
        line.SerialNo,
        line.QtyReturned,
        line.UomId,
        line.Uom.Code,
        line.UnitCost,
        line.GoodsReceiptPoLineId);
}
