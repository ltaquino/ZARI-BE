namespace ZARI.Application.Features.SystemModule.Currencies.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Domain.Common;

public sealed record CreateCurrencyCommand(string Code, string? Name, string Status) : ICommand<Result<CurrencyResponse>>;
