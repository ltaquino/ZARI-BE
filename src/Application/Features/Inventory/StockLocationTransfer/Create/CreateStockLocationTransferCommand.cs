namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Common;

public sealed record StockLocationTransferLineInput(
    Guid ItemId,
    string? BatchNo,
    string? SerialNo,
    Guid FromLocationId,
    Guid ToLocationId,
    decimal Qty);

public sealed record CreateStockLocationTransferCommand(
    string BranchId,
    Guid WarehouseId,
    DateTimeOffset TransferDate,
    string? Remarks,
    string? CreatedBy,
    List<StockLocationTransferLineInput> Lines) : ICommand<Result<StockLocationTransferResponse>>;
