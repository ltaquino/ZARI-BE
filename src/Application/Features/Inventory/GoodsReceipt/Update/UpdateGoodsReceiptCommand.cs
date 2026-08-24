namespace ZARI.Application.Features.Inventory.GoodsReceipts.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsReceipts.Create;
using ZARI.Application.Features.Inventory.GoodsReceipts.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateGoodsReceiptCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    string ReceiptType,
    string? ReceivedBy,
    DateTimeOffset GrDate,
    string? Remarks,
    string? GoodsIssueRefNo,
    string? GoodsIssueId,
    string? ReasonCode,
    string? UpdatedBy,
    List<GoodsReceiptLineInput> Lines) : ICommand<Result<GoodsReceiptResponse>>;
