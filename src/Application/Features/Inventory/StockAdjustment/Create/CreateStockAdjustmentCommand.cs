namespace ZARI.Application.Features.Inventory.StockAdjustments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record StockAdjustmentLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal QtyBefore, decimal QtyAfter, decimal UnitCost);

public sealed record CreateStockAdjustmentCommand(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset AdjustmentDate,
    string? ReasonCode,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<StockAdjustmentLineInput> Lines,
    // Idempotency key from an offline client (ZARI Mobile): a repeat with the same key returns the first one's document.
    Guid? ClientReferenceId = null) : ICommand<Result<StockAdjustmentResponse>>;
