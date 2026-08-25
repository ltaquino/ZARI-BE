namespace ZARI.Application.Features.Purchasing.GoodsReturns.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.Create;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateGoodsReturnCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? GoodsReceiptPoId,
    string ReasonCode,
    DateTimeOffset ReturnDate,
    string? Remarks,
    string? UpdatedBy,
    List<GoodsReturnLineInput> Lines) : ICommand<Result<GoodsReturnResponse>>;
