namespace ZARI.Application.Features.Accounting.GlAccounts.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateGlAccountCommand(
    Guid Id,
    string Code,
    string Name,
    string AccountType,
    string NormalBalance,
    Guid? ParentAccountId,
    string Status) : ICommand;
