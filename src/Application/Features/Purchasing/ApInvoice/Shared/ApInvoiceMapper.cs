namespace ZARI.Application.Features.Purchasing.ApInvoices.Shared;

using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Entities;

internal static class ApInvoiceMapper
{
    // amountPaid defaults to 0 since every ApInvoice CQRS action other than Get/GetAll only ever
    // operates on an invoice in a status that implies nothing's been paid yet (PARTIALLY_PAID/PAID
    // are only reachable via Outgoing Payment, which blocks every one of ApInvoice's own mutations).
    public static ApInvoiceResponse ToResponse(ApInvoice invoice, decimal amountPaid = 0) => new(
        invoice.Id,
        invoice.InvoiceNo,
        invoice.BranchId,
        invoice.SupplierId,
        invoice.Supplier.Code,
        invoice.Supplier.Name,
        invoice.InvoiceType,
        invoice.GoodsReceiptPoId,
        invoice.GoodsReceiptPo?.GrpoNo,
        invoice.SupplierInvoiceNo,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.Status,
        invoice.Remarks,
        invoice.Lines.Select(ToLineResponse).ToList(),
        invoice.ExpenseLines.Select(ToExpenseLineResponse).ToList(),
        amountPaid,
        invoice.CancelledBy,
        invoice.CancelledAt,
        invoice.CancelReason,
        invoice.CreatedAt,
        invoice.CreatedBy);

    private static ApInvoiceLineResponse ToLineResponse(ApInvoiceLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Qty,
        line.UomId,
        line.Uom.Code,
        line.UnitCost);

    private static ApInvoiceExpenseLineResponse ToExpenseLineResponse(ApInvoiceExpenseLine line) => new(
        line.Id,
        line.GlAccountId,
        line.GlAccount.Code,
        line.GlAccount.Name,
        line.Description,
        line.Amount);
}
