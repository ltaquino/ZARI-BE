namespace ZARI.Application.Features.Sales.PaymentMethods.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Domain.Common;

public sealed record GetAllPaymentMethodsQuery : IQuery<Result<List<PaymentMethodResponse>>>;
