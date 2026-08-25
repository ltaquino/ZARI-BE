namespace ZARI.Application.Features.Purchasing.Suppliers.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetSupplierQuery(Guid Id) : IQuery<Result<SupplierResponse>>;

public sealed record SupplierResponse(
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
    string Status,
    DateTimeOffset CreatedAt);
