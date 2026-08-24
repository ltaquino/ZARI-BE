namespace ZARI.Application.Features.Inventory.GoodsIssues.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.Create;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateGoodsIssueCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    string ReferenceType,
    string? DestBranchId,
    Guid? DestWarehouseId,
    string? ReasonCode,
    DateTimeOffset GiDate,
    string? Remarks,
    string? StockTransferRequestRefNo,
    string? StockTransferRequestId,
    string? UpdatedBy,
    List<GoodsIssueLineInput> Lines) : ICommand<Result<GoodsIssueResponse>>;
