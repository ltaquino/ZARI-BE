namespace ZARI.Application.Features.Purchasing.ApInvoices.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

// VatType is classification-only (Purchase Book reporting — see ApInvoiceLine.VatType) — omitted
// (null), an ITEM line defaults from its own Item's VatType; an EXPENSE line defaults to VATABLE.
public sealed record ApInvoiceLineInput(Guid ItemId, decimal Qty, Guid UomId, decimal UnitCost, Guid? GoodsReceiptPoLineId, string? VatType = null);
public sealed record ApInvoiceExpenseLineInput(Guid GlAccountId, string Description, decimal Amount, string? VatType = null);

public sealed record CreateApInvoiceCommand(
    string BranchId,
    Guid SupplierId,
    string InvoiceType,
    Guid? GoodsReceiptPoId,
    string SupplierInvoiceNo,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<ApInvoiceLineInput> Lines,
    List<ApInvoiceExpenseLineInput> ExpenseLines) : ICommand<Result<ApInvoiceResponse>>;
