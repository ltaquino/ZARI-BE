namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record GoodsReceiptPoLineInput(Guid ItemId, string? BatchNo, string? SerialNo, decimal QtyReceived, Guid UomId, decimal UnitCost, Guid? LocationId, Guid? PurchaseOrderLineId);

public sealed record CreateGoodsReceiptPoCommand(
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? PurchaseOrderId,
    string? SupplierInvoiceNo,
    DateTimeOffset ReceiptDate,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<GoodsReceiptPoLineInput> Lines) : ICommand<Result<GoodsReceiptPoResponse>>;
