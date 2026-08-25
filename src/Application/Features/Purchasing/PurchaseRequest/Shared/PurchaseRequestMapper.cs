namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Shared;

using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Entities;

internal static class PurchaseRequestMapper
{
    public static PurchaseRequestResponse ToResponse(PurchaseRequest request) => new(
        request.Id,
        request.RequestNo,
        request.BranchId,
        request.RequestDate,
        request.Status,
        request.Remarks,
        request.Lines.Select(ToLineResponse).ToList(),
        request.CancelledBy,
        request.CancelledAt,
        request.CancelReason,
        request.CreatedAt,
        request.CreatedBy);

    private static PurchaseRequestLineResponse ToLineResponse(PurchaseRequestLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.QtyRequested,
        line.UomId,
        line.Uom.Code,
        line.NeededByDate);
}
