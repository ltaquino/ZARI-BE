namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Domain.Common;

public sealed record CreateStatutoryDiscountTypeCommand(
    string Code,
    string Name,
    decimal DiscountPct,
    bool IsVatExempt,
    string RequiredIdLabel,
    string Status) : ICommand<Result<StatutoryDiscountTypeResponse>>;
