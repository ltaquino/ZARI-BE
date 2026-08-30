namespace ZARI.Application.Features.Sales.CustomerPayments.Shared;

using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Entities;

internal static class CustomerPaymentMapper
{
    public static CustomerPaymentResponse ToResponse(CustomerPayment payment) => new(
        payment.Id,
        payment.PaymentNo,
        payment.BranchId,
        payment.CustomerId,
        payment.Customer.Name,
        payment.PaymentMethod,
        payment.CashAccountId,
        payment.CashAccount.Code,
        payment.CashAccount.Name,
        payment.PaymentDate,
        payment.ReferenceNo,
        payment.Status,
        payment.Remarks,
        payment.Lines.Sum(l => l.AmountApplied),
        payment.Lines.Select(ToLineResponse).ToList(),
        payment.CostCenterId,
        payment.CancelledBy,
        payment.CancelledAt,
        payment.CancelReason,
        payment.CreatedAt,
        payment.CreatedBy);

    private static CustomerPaymentLineResponse ToLineResponse(CustomerPaymentLine line) => new(
        line.Id,
        line.SalesInvoiceId,
        line.SalesInvoice.InvoiceNo,
        line.AmountApplied);
}
