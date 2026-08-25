namespace ZARI.Application.Features.SystemModule.Currencies.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Domain.Common;

public sealed record GetAllCurrenciesQuery : IQuery<Result<List<CurrencyResponse>>>;
