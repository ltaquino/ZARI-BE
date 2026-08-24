namespace ZARI.Application.Features.Inventory.StockTransferRequests.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.Create;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateStockTransferRequestCommand(
    Guid Id,
    string SourceBranchId,
    Guid SourceWarehouseId,
    string DestBranchId,
    Guid DestWarehouseId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? UpdatedBy,
    List<StockTransferRequestLineInput> Lines) : ICommand<Result<StockTransferRequestResponse>>;
