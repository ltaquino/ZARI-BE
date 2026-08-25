namespace ZARI.Application.Features.Accounting.ExchangeRates.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateExchangeRateCommand(Guid Id, string CurrencyId, DateTimeOffset RateDate, decimal RateToBase) : ICommand;
