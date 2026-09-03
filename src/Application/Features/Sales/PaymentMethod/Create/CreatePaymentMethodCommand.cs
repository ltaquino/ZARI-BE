namespace ZARI.Application.Features.Sales.PaymentMethods.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Domain.Common;

public sealed record CreatePaymentMethodCommand(
    string Code,
    string Name,
    Guid GlAccountId,
    bool RequiresReferenceNo,
    string? ReferenceNoLabel,
    bool RequiresBankOrPartnerName,
    int DisplayOrder,
    string Status) : ICommand<Result<PaymentMethodResponse>>;
