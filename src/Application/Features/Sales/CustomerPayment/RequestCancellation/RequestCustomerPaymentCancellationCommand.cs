namespace ZARI.Application.Features.Sales.CustomerPayments.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RequestCustomerPaymentCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<CustomerPaymentResponse>>;
