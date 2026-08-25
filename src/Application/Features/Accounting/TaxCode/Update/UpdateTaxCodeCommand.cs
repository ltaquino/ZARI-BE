namespace ZARI.Application.Features.Accounting.TaxCodes.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateTaxCodeCommand(string Code, string? Name, decimal Rate, string TaxType, Guid? GlAccountId) : ICommand;
