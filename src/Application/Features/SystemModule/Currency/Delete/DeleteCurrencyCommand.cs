namespace ZARI.Application.Features.SystemModule.Currencies.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteCurrencyCommand(string Id) : ICommand;
