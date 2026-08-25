namespace ZARI.Application.Features.Accounting.ExchangeRates.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Domain.Common;

public sealed record CreateExchangeRateCommand(string CurrencyId, DateTimeOffset RateDate, decimal RateToBase) : ICommand<Result<ExchangeRateResponse>>;
