namespace ZARI.Application.Features.Purchasing.ApInvoices.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record ApInvoiceLineInput(Guid ItemId, decimal Qty, Guid UomId, decimal UnitCost, Guid? GoodsReceiptPoLineId);
public sealed record ApInvoiceExpenseLineInput(Guid GlAccountId, string Description, decimal Amount);

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
