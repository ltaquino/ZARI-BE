namespace ZARI.Application.Features.Accounting.TaxCodes.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetTaxCodeQuery(string Code) : IQuery<Result<TaxCodeResponse>>;

public sealed record TaxCodeResponse(string Id, string Code, string? Name, decimal Rate, string TaxType, Guid? GlAccountId, DateTimeOffset CreatedAt);
