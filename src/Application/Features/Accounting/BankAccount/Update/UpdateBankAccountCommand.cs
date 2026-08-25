namespace ZARI.Application.Features.Accounting.BankAccounts.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateBankAccountCommand(Guid Id, string BranchId, string AccountName, string AccountNumber, string BankName, Guid GlAccountId, string? CurrencyId) : ICommand;
