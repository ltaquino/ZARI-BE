namespace ZARI.Application.Features.Sales.PaymentMethods.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePaymentMethodCommand(
    Guid Id,
    string Code,
    string Name,
    Guid GlAccountId,
    bool RequiresReferenceNo,
    string? ReferenceNoLabel,
    bool RequiresBankOrPartnerName,
    int DisplayOrder,
    string Status) : ICommand;
