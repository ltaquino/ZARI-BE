namespace ZARI.Application.Features.Sales.SalesInvoices.Shared;

using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Entities;

internal static class SalesInvoiceMapper
{
    /// <param name="amountPaid">Live-computed via SalesInvoicePaymentBalance — pass 0 when the caller has no payment data at hand (amountPaid/balance are then reported as 0/full-total, never wrong-but-stale).</param>
    /// <param name="payments">Only the Get-by-id query populates this (receipt printing needs it) — GetAll passes null/empty to avoid an N+1 fan-out on the list view.</param>
    public static SalesInvoiceResponse ToResponse(SalesInvoice invoice, decimal amountPaid = 0, List<SalesInvoicePaymentSummary>? payments = null)
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
        invoice.PosTerminalId,
        invoice.Lines.Select(ToLineResponse).ToList(),
        invoice.CancelledBy,
        invoice.CancelledAt,
        invoice.CancelReason,
        invoice.CreatedAt,
        invoice.CreatedBy,
        invoice.PrintCount,
        invoice.FirstPrintedAt,
        invoice.LastPrintedAt,
        payments ?? []);
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
        line.DeliveryOrderLineId,
        line.SerialNo);
}
