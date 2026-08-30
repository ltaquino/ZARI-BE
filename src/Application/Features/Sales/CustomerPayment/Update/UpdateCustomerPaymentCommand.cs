namespace ZARI.Application.Features.Sales.CustomerPayments.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateCustomerPaymentCommand(
    Guid Id,
    string PaymentMethod,
    Guid CashAccountId,
    DateTimeOffset PaymentDate,
    string? ReferenceNo,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<CustomerPaymentLineInput> Lines) : ICommand<Result<CustomerPaymentResponse>>;
