namespace ZARI.Application.Features.Inventory.StockTransferRequests.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record StockTransferRequestLineInput(Guid ItemId, decimal QtyRequested, Guid UomId);

public sealed record CreateStockTransferRequestCommand(
    string SourceBranchId,
    Guid SourceWarehouseId,
    string DestBranchId,
    Guid DestWarehouseId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? CreatedBy,
    List<StockTransferRequestLineInput> Lines) : ICommand<Result<StockTransferRequestResponse>>;
