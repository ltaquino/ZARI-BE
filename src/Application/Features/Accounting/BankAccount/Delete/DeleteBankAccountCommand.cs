namespace ZARI.Application.Features.Accounting.BankAccounts.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteBankAccountCommand(Guid Id) : ICommand;
