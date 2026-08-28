namespace ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllOutgoingPaymentsQuery : IQuery<Result<List<OutgoingPaymentResponse>>>;

public sealed record OutgoingPaymentLineResponse(
    Guid Id,
    Guid ApInvoiceId,
    string ApInvoiceNo,
    string SupplierInvoiceNo,
    decimal Amount);

public sealed record OutgoingPaymentResponse(
    Guid Id,
    string PaymentNo,
    string BranchId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid BankAccountId,
    string BankAccountName,
    string BankName,
    DateTimeOffset PaymentDate,
    string? RefNo,
    string Status,
    string? Remarks,
    decimal TotalAmount,
    List<OutgoingPaymentLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
