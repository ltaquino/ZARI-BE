namespace ZARI.Application.Features.Sales.CustomerPayments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record GetCustomerPaymentQuery(Guid Id) : IQuery<Result<CustomerPaymentResponse>>;
