namespace ZARI.Application.Features.Sales.CustomerPayments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllCustomerPaymentsQuery : IQuery<Result<List<CustomerPaymentResponse>>>;

public sealed record CustomerPaymentLineResponse(
    Guid Id,
    Guid SalesInvoiceId,
    string SalesInvoiceNo,
    decimal AmountApplied);

public sealed record CustomerPaymentResponse(
    Guid Id,
    string PaymentNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    string PaymentMethod,
    Guid CashAccountId,
    string CashAccountCode,
    string CashAccountName,
    DateTimeOffset PaymentDate,
    string? ReferenceNo,
    string Status,
    string? Remarks,
    decimal TotalAmount,
    List<CustomerPaymentLineResponse> Lines,
    Guid? CostCenterId,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
