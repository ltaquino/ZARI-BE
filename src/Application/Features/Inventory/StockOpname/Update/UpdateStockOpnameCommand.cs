namespace ZARI.Application.Features.Inventory.StockOpnames.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.Create;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateStockOpnameCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset CountDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<StockOpnameLineInput> Lines) : ICommand<Result<StockOpnameResponse>>;
