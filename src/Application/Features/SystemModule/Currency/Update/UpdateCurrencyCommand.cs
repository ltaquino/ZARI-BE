namespace ZARI.Application.Features.SystemModule.Currencies.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCurrencyCommand(string Id, string Code, string? Name, string Status) : ICommand;
