namespace ZARI.Application.Features.SystemModule.Companies.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCompanyQuery : IQuery<Result<CompanyResponse>>;

public sealed record CompanyResponse(
    Guid Id,
    string Code,
    string Name,
    string? TaxId,
    string BaseCurrencyId,
    DateTimeOffset CreatedAt,
    string? RegisteredAddress,
    string? TradeName,
    string? VatRegistrationType,
    decimal? MaxUnapprovedDiscountPct,
    bool SalesOrderQuickPostEnabled,
    bool DeliveryQuickPostEnabled,
    bool SalesInvoiceQuickPostEnabled,
    bool CustomerPaymentQuickPostEnabled,
    bool SalesReturnQuickPostEnabled);
