namespace ZARI.Application.Features.Accounting.ExchangeRates.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteExchangeRateCommand(Guid Id) : ICommand;
