namespace ZARI.Application.Features.Sales.CustomerPayments.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record CancelCustomerPaymentCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<CustomerPaymentResponse>>;
