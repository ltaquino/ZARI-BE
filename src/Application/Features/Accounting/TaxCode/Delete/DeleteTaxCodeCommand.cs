namespace ZARI.Application.Features.Accounting.TaxCodes.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteTaxCodeCommand(string Code) : ICommand;
