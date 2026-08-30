namespace ZARI.Application.Features.Sales.SalesInvoices.Shared;

using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Entities;

internal static class SalesInvoiceMapper
{
    /// <param name="amountPaid">Live-computed via SalesInvoicePaymentBalance — pass 0 when the caller has no payment data at hand (amountPaid/balance are then reported as 0/full-total, never wrong-but-stale).</param>
    public static SalesInvoiceResponse ToResponse(SalesInvoice invoice, decimal amountPaid = 0)
    {
        var invoiceTotal = SalesInvoicePaymentBalance.GetInvoiceTotal(invoice);
        return new(
        invoice.Id,
        invoice.InvoiceNo,
        invoice.BranchId,
        invoice.CustomerId,
        invoice.Customer.Name,
        invoice.DeliveryOrderId,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.Status,
        invoice.Remarks,
        invoice.DiscountPct,
        invoice.BirOrSeriesNumber,
        invoice.PaidAmount,
        amountPaid,
        invoiceTotal - amountPaid,
        invoice.CostCenterId,
        invoice.Lines.Select(ToLineResponse).ToList(),
        invoice.CancelledBy,
        invoice.CancelledAt,
        invoice.CancelReason,
        invoice.CreatedAt,
        invoice.CreatedBy);
    }

    private static SalesInvoiceLineResponse ToLineResponse(SalesInvoiceLine line) => new(
        line.Id,
        line.ItemId,
        line.Item.Code,
        line.Item.Name,
        line.Item.Description,
        line.Qty,
        line.UomId,
        line.Uom.Code,
        line.UnitPrice,
        line.DiscountPct,
        line.DiscountSourceType,
        line.DiscountSourceId,
        line.VatType,
        line.StatutoryDiscountTypeId,
        line.StatutoryDiscountType?.Name,
        line.StatutoryIdNumber,
        line.DeliveryOrderLineId);
}
