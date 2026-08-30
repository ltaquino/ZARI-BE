namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetStatutoryDiscountTypeQuery(Guid Id) : IQuery<Result<StatutoryDiscountTypeResponse>>;

public sealed record StatutoryDiscountTypeResponse(
    Guid Id,
    string Code,
    string Name,
    decimal DiscountPct,
    bool IsVatExempt,
    string RequiredIdLabel,
    string Status,
    DateTimeOffset CreatedAt);
