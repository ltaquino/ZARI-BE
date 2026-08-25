namespace ZARI.Application.Features.Purchasing.GoodsReturns.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record GoodsReturnLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal QtyReturned, Guid UomId, decimal UnitCost);

public sealed record CreateGoodsReturnCommand(
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? GoodsReceiptPoId,
    string ReasonCode,
    DateTimeOffset ReturnDate,
    string? Remarks,
    string? CreatedBy,
    List<GoodsReturnLineInput> Lines) : ICommand<Result<GoodsReturnResponse>>;
