namespace ZARI.Application.Features.Inventory.GoodsIssues.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Domain.Common;

public sealed record GoodsIssueLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal QtyIssued, Guid UomId, decimal UnitCost);

public sealed record CreateGoodsIssueCommand(
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
    string? CreatedBy,
    List<GoodsIssueLineInput> Lines) : ICommand<Result<GoodsIssueResponse>>;
