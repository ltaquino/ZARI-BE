namespace ZARI.Application.Features.Accounting.ExchangeRates.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Domain.Common;

public sealed record GetAllExchangeRatesQuery : IQuery<Result<List<ExchangeRateResponse>>>;
