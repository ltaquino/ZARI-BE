namespace ZARI.Application.Features.Sales.CustomerPayments.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteCustomerPaymentCommand(Guid Id) : ICommand<Result>;
