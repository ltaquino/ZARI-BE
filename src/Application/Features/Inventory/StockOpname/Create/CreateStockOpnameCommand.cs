namespace ZARI.Application.Features.Inventory.StockOpnames.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record StockOpnameLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal SystemQty, decimal CountedQty, decimal UnitCost);

public sealed record CreateStockOpnameCommand(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset CountDate,
    string? Remarks,
    string? CreatedBy,
    List<StockOpnameLineInput> Lines) : ICommand<Result<StockOpnameResponse>>;
