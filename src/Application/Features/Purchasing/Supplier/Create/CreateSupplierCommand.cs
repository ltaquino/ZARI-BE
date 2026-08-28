namespace ZARI.Application.Features.Purchasing.Suppliers.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Domain.Common;

public sealed record CreateSupplierCommand(
    string Code,
    string Name,
    string? TaxId,
    int? PaymentTermsDays,
    string? CurrencyId,
    Guid? ApAccountId,
    string? Address,
    string? ContactPerson,
    string? ContactNumber,
    string? Email,
    string Status) : ICommand<Result<SupplierResponse>>;
