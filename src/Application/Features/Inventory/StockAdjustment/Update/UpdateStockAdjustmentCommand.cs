namespace ZARI.Application.Features.Inventory.StockAdjustments.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.Create;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateStockAdjustmentCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset AdjustmentDate,
    string? ReasonCode,
    string? Remarks,
    string? UpdatedBy,
    List<StockAdjustmentLineInput> Lines) : ICommand<Result<StockAdjustmentResponse>>;
