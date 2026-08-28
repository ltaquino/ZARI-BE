namespace ZARI.Application.Features.Inventory.GoodsIssues.Shared;

using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Entities;

internal static class GoodsIssueMapper
{
    public static GoodsIssueResponse ToResponse(GoodsIssue issue) => new(
        issue.Id,
        issue.GiNo,
        issue.BranchId,
        issue.WarehouseId,
        issue.ReferenceType,
        issue.DestBranchId,
        issue.DestWarehouseId,
        issue.ReasonCode,
        issue.GiDate,
        issue.Status,
        issue.ShipmentStatus,
        issue.Remarks,
        issue.Lines.Select(ToLineResponse).ToList(),
        issue.StockTransferRequestRefNo,
        issue.StockTransferRequestId,
        issue.CostCenterId,
        issue.CancelledBy,
        issue.CancelledAt,
        issue.CancelReason,
        issue.CreatedAt,
        issue.CreatedBy);

    private static GoodsIssueLineResponse ToLineResponse(GoodsIssueLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.BatchNo,
        line.SerialNo,
        line.QtyIssued,
        line.UomId,
        line.Uom.Code,
        line.UnitCost);
}
