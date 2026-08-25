namespace ZARI.Application.Features.Purchasing.Suppliers.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Code,
    string Name,
    string? TaxId,
    string? PaymentTerms,
    string? CurrencyId,
    Guid? ApAccountId,
    string? Address,
    string? ContactPerson,
    string? ContactNumber,
    string? Email,
    string Status) : ICommand;
