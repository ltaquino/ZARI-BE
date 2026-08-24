namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Create;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateStockLocationTransferCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset TransferDate,
    string? Remarks,
    string? UpdatedBy,
    List<StockLocationTransferLineInput> Lines) : ICommand<Result<StockLocationTransferResponse>>;
