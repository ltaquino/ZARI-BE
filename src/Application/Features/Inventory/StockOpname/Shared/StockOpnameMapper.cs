namespace ZARI.Application.Features.Inventory.StockOpnames.Shared;

using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Entities;

internal static class StockOpnameMapper
{
    public static StockOpnameResponse ToResponse(StockOpname opname) => new(
        opname.Id,
        opname.OpnameNo,
        opname.BranchId,
        opname.WarehouseId,
        opname.CountDate,
        opname.Status,
        opname.Remarks,
        opname.Lines.Select(ToLineResponse).ToList(),
        opname.CostCenterId,
        opname.PostedBy,
        opname.CancelledBy,
        opname.CancelledAt,
        opname.CancelReason,
        opname.CreatedAt,
        opname.CreatedBy);

    private static StockOpnameLineResponse ToLineResponse(StockOpnameLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Item.BaseUom.Code,
        line.BatchNo,
        line.SerialNo,
        line.SystemQty,
        line.CountedQty,
        line.VarianceQty,
        line.UnitCost);
}
