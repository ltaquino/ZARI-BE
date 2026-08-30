namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateStatutoryDiscountTypeCommand(
    Guid Id,
    string Code,
    string Name,
    decimal DiscountPct,
    bool IsVatExempt,
    string RequiredIdLabel,
    string Status) : ICommand;
