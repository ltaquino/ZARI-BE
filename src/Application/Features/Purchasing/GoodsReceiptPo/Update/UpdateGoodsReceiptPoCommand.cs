namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Create;
using ZARI.Application.Features.Purchasing.GoodsReceiptPos.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateGoodsReceiptPoCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    Guid SupplierId,
    Guid? PurchaseOrderId,
    string? SupplierInvoiceNo,
    DateTimeOffset ReceiptDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<GoodsReceiptPoLineInput> Lines) : ICommand<Result<GoodsReceiptPoResponse>>;
