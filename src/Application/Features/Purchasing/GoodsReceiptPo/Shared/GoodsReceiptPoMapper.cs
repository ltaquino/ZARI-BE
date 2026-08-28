namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Shared;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Entities;

internal static class GoodsReceiptPoMapper
{
    public static GoodsReceiptPoResponse ToResponse(GoodsReceiptPo receipt) => new(
        receipt.Id,
        receipt.GrpoNo,
        receipt.BranchId,
        receipt.WarehouseId,
        receipt.SupplierId,
        receipt.Supplier.Code,
        receipt.Supplier.Name,
        receipt.PurchaseOrderId,
        receipt.SupplierInvoiceNo,
        receipt.ReceiptDate,
        receipt.Status,
        receipt.Remarks,
        receipt.Lines.Select(ToLineResponse).ToList(),
        receipt.CostCenterId,
        receipt.CancelledBy,
        receipt.CancelledAt,
        receipt.CancelReason,
        receipt.CreatedAt,
        receipt.CreatedBy);

    private static GoodsReceiptPoLineResponse ToLineResponse(GoodsReceiptPoLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.BatchNo,
        line.SerialNo,
        line.QtyReceived,
        line.UomId,
        line.Uom.Code,
        line.UnitCost,
        line.LocationId,
        line.PurchaseOrderLineId);
}
