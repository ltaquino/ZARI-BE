namespace ZARI.Application.Features.Accounting.TaxCodes.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Domain.Common;

public sealed record CreateTaxCodeCommand(string Code, string? Name, decimal Rate, string TaxType, Guid? GlAccountId) : ICommand<Result<TaxCodeResponse>>;
