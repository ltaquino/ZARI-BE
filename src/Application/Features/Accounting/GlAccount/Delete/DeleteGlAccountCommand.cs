namespace ZARI.Application.Features.Accounting.GlAccounts.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteGlAccountCommand(Guid Id) : ICommand;
