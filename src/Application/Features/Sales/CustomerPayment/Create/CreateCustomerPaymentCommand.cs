namespace ZARI.Application.Features.Sales.CustomerPayments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record CustomerPaymentLineInput(Guid SalesInvoiceId, decimal AmountApplied);

public sealed record CreateCustomerPaymentCommand(
    string BranchId,
    Guid CustomerId,
    string PaymentMethod,
    Guid CashAccountId,
    DateTimeOffset PaymentDate,
    string? ReferenceNo,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<CustomerPaymentLineInput> Lines) : ICommand<Result<CustomerPaymentResponse>>;
