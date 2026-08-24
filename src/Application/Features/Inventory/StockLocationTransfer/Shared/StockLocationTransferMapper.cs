namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Shared;

using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Entities;

internal static class StockLocationTransferMapper
{
    public static StockLocationTransferResponse ToResponse(StockLocationTransfer transfer) => new(
        transfer.Id,
        transfer.TransferNo,
        transfer.BranchId,
        transfer.WarehouseId,
        transfer.TransferDate,
        transfer.Status,
        transfer.Remarks,
        transfer.Lines.Select(ToLineResponse).ToList(),
        transfer.PostedBy,
        transfer.CancelledBy,
        transfer.CancelledAt,
        transfer.CancelReason,
        transfer.CreatedAt,
        transfer.CreatedBy);

    private static StockLocationTransferLineResponse ToLineResponse(StockLocationTransferLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.BatchNo,
        line.SerialNo,
        line.FromLocationId,
        FormatLocation(line.FromLocation),
        line.ToLocationId,
        FormatLocation(line.ToLocation),
        line.Qty);

    private static string FormatLocation(StorageLocation location)
    {
        var parts = new[] { location.Zone, location.Aisle, location.Rack, location.BinCode }.Where(p => !string.IsNullOrWhiteSpace(p));
        var label = string.Join("-", parts);
        return string.IsNullOrEmpty(label) ? location.Id.ToString() : label;
    }
}
