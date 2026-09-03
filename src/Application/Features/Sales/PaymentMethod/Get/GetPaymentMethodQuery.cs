namespace ZARI.Application.Features.Sales.PaymentMethods.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPaymentMethodQuery(Guid Id) : IQuery<Result<PaymentMethodResponse>>;

public sealed record PaymentMethodResponse(
    Guid Id,
    string Code,
    string Name,
    Guid GlAccountId,
    string GlAccountCode,
    string GlAccountName,
    bool RequiresReferenceNo,
    string? ReferenceNoLabel,
    bool RequiresBankOrPartnerName,
    int DisplayOrder,
    string Status,
    DateTimeOffset CreatedAt);
