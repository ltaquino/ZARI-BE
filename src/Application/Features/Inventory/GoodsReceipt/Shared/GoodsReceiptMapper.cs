namespace ZARI.Application.Features.Inventory.GoodsReceipts.Shared;

using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Entities;

internal static class GoodsReceiptMapper
{
    public static GoodsReceiptResponse ToResponse(GoodsReceipt receipt) => new(
        receipt.Id,
        receipt.GrNo,
        receipt.BranchId,
        receipt.WarehouseId,
        receipt.ReceiptType,
        receipt.ReceivedBy,
        receipt.GrDate,
        receipt.Status,
        receipt.Remarks,
        receipt.Lines.Select(ToLineResponse).ToList(),
        receipt.GoodsIssueRefNo,
        receipt.GoodsIssueId,
        receipt.ReasonCode,
        receipt.CostCenterId,
        receipt.CancelledBy,
        receipt.CancelledAt,
        receipt.CancelReason,
        receipt.CreatedAt,
        receipt.CreatedBy);

    private static GoodsReceiptLineResponse ToLineResponse(GoodsReceiptLine line) => new(
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
        line.LocationId);
}
