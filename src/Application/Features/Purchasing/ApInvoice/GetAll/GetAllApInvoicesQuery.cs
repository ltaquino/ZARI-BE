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
    decimal UnitCost,
    Guid? GoodsReceiptPoLineId,
    string VatType);

public sealed record ApInvoiceExpenseLineResponse(
    Guid Id,
    Guid GlAccountId,
    string GlAccountCode,
    string GlAccountName,
    string Description,
    decimal Amount,
    string VatType);

public sealed record ApInvoiceResponse(
    Guid Id,
    string InvoiceNo,
    string BranchId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    string InvoiceType,
    Guid? GoodsReceiptPoId,
    string? GrpoNo,
    string SupplierInvoiceNo,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string Status,
    string? Remarks,
    List<ApInvoiceLineResponse> Lines,
    List<ApInvoiceExpenseLineResponse> ExpenseLines,
    decimal AmountPaid,
    Guid? CostCenterId,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
