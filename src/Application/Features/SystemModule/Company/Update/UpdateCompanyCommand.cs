namespace ZARI.Application.Features.SystemModule.Companies.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCompanyCommand(
    string Code,
    string Name,
    string? TaxId,
    string BaseCurrencyId,
    string? RegisteredAddress,
    string? TradeName,
    string? VatRegistrationType,
    decimal? MaxUnapprovedDiscountPct,
    bool SalesOrderQuickPostEnabled,
    bool DeliveryQuickPostEnabled,
    bool SalesInvoiceQuickPostEnabled,
    bool CustomerPaymentQuickPostEnabled,
    bool SalesReturnQuickPostEnabled) : ICommand;
