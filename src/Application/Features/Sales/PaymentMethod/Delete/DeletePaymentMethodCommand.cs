namespace ZARI.Application.Features.Sales.PaymentMethods.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePaymentMethodCommand(Guid Id) : ICommand;
