namespace ZARI.Application.Features.SystemModule.Currencies.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCurrencyQuery(string Id) : IQuery<Result<CurrencyResponse>>;

public sealed record CurrencyResponse(string Id, string Code, string? Name, string Status, DateTimeOffset CreatedAt);
