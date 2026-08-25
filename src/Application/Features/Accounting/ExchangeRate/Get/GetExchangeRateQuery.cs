namespace ZARI.Application.Features.Accounting.ExchangeRates.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetExchangeRateQuery(Guid Id) : IQuery<Result<ExchangeRateResponse>>;

public sealed record ExchangeRateResponse(Guid Id, string CurrencyId, DateTimeOffset RateDate, decimal RateToBase, DateTimeOffset CreatedAt);
