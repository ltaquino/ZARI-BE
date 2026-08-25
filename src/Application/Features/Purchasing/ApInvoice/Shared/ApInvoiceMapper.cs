namespace ZARI.Application.Features.Purchasing.ApInvoices.Shared;

using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Entities;

internal static class ApInvoiceMapper
{
    public static ApInvoiceResponse ToResponse(ApInvoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNo,
        invoice.BranchId,
        invoice.SupplierId,
        invoice.Supplier.Code,
        invoice.Supplier.Name,
        invoice.GoodsReceiptPoId,
        invoice.GoodsReceiptPo.GrpoNo,
        invoice.SupplierInvoiceNo,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.Status,
        invoice.Remarks,
        invoice.Lines.Select(ToLineResponse).ToList(),
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
}
