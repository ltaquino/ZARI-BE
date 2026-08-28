namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;

using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Entities;

internal static class OutgoingPaymentMapper
{
    public static OutgoingPaymentResponse ToResponse(OutgoingPayment payment) => new(
        payment.Id,
        payment.PaymentNo,
        payment.BranchId,
        payment.SupplierId,
        payment.Supplier.Code,
        payment.Supplier.Name,
        payment.BankAccountId,
        payment.BankAccount.AccountName,
        payment.BankAccount.BankName,
        payment.PaymentDate,
        payment.RefNo,
        payment.Status,
        payment.Remarks,
        payment.Lines.Sum(l => l.Amount),
        payment.Lines.Select(ToLineResponse).ToList(),
        payment.CancelledBy,
        payment.CancelledAt,
        payment.CancelReason,
        payment.CreatedAt,
        payment.CreatedBy);

    private static OutgoingPaymentLineResponse ToLineResponse(OutgoingPaymentLine line) => new(
        line.Id,
        line.ApInvoiceId,
        line.ApInvoice.InvoiceNo,
        line.ApInvoice.SupplierInvoiceNo,
        line.Amount);
}
