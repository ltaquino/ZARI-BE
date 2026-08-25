namespace ZARI.Application.Features.Purchasing.ApInvoices.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllApInvoicesQuery : IQuery<Result<List<ApInvoiceResponse>>>;

public sealed record ApInvoiceLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal Qty,
    Guid UomId,
    string UomCode,
    decimal UnitCost);

public sealed record ApInvoiceResponse(
    Guid Id,
    string InvoiceNo,
    string BranchId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid GoodsReceiptPoId,
    string GrpoNo,
    string SupplierInvoiceNo,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string Status,
    string? Remarks,
    List<ApInvoiceLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
