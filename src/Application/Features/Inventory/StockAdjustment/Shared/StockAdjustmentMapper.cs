namespace ZARI.Application.Features.Inventory.StockAdjustments.Shared;

using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Entities;

internal static class StockAdjustmentMapper
{
    public static StockAdjustmentResponse ToResponse(StockAdjustment adjustment) => new(
        adjustment.Id,
        adjustment.AdjustmentNo,
        adjustment.BranchId,
        adjustment.WarehouseId,
        adjustment.AdjustmentDate,
        adjustment.ReasonCode,
        adjustment.Status,
        adjustment.Remarks,
        adjustment.Lines.Select(ToLineResponse).ToList(),
        adjustment.CancelledBy,
        adjustment.CancelledAt,
        adjustment.CancelReason,
        adjustment.CreatedAt,
        adjustment.CreatedBy);

    private static StockAdjustmentLineResponse ToLineResponse(StockAdjustmentLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Item.BaseUom.Code,
        line.BatchNo,
        line.SerialNo,
        line.QtyBefore,
        line.QtyAfter,
        line.VarianceQty,
        line.UnitCost);
}
