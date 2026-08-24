namespace ZARI.Application.Features.Inventory.StockTransferRequests.Shared;

using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Entities;

internal static class StockTransferRequestMapper
{
    public static StockTransferRequestResponse ToResponse(StockTransferRequest request) => new(
        request.Id,
        request.RequestNo,
        request.SourceBranchId,
        request.SourceWarehouseId,
        request.DestBranchId,
        request.DestWarehouseId,
        request.RequestDate,
        request.Status,
        request.Remarks,
        request.Lines.Select(ToLineResponse).ToList(),
        request.DeclinedBy,
        request.DeclinedAt,
        request.DeclineReason,
        request.CancelledBy,
        request.CancelledAt,
        request.CancelReason,
        request.CreatedAt,
        request.CreatedBy);

    private static StockTransferRequestLineResponse ToLineResponse(StockTransferRequestLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.QtyRequested,
        line.UomId,
        line.Uom.Code);
}
