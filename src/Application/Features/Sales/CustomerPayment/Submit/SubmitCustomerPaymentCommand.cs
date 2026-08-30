namespace ZARI.Application.Features.Sales.CustomerPayments.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitCustomerPaymentCommand(Guid Id, string RequestedBy) : ICommand<Result<CustomerPaymentResponse>>;
