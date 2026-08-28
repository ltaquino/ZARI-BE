namespace ZARI.Application.Features.Inventory.GoodsReceipts.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record GoodsReceiptLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal QtyReceived, Guid UomId, decimal UnitCost, Guid? LocationId);

public sealed record CreateGoodsReceiptCommand(
    string BranchId,
    Guid WarehouseId,
    string ReceiptType,
    string? ReceivedBy,
    DateTimeOffset GrDate,
    string? Remarks,
    string? GoodsIssueRefNo,
    string? GoodsIssueId,
    string? ReasonCode,
    Guid? CostCenterId,
    string? CreatedBy,
    List<GoodsReceiptLineInput> Lines) : ICommand<Result<GoodsReceiptResponse>>;
