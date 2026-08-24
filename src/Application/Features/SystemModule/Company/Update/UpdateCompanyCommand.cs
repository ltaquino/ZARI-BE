namespace ZARI.Application.Features.SystemModule.Companies.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCompanyCommand(string Code, string Name, string? TaxId, string BaseCurrencyId) : ICommand;
