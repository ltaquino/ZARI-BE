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
    Guid? CostCenterId,
    string? CreatedBy,
    List<StockOpnameLineInput> Lines,
    // Idempotency key from an offline client (ZARI Mobile): a repeat with the same key returns the first one's document.
    Guid? ClientReferenceId = null) : ICommand<Result<StockOpnameResponse>>;
