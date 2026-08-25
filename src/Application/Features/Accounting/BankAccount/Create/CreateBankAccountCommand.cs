namespace ZARI.Application.Features.Accounting.BankAccounts.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.BankAccounts.Get;
using ZARI.Domain.Common;

public sealed record CreateBankAccountCommand(string BranchId, string AccountName, string AccountNumber, string BankName, Guid GlAccountId, string? CurrencyId) : ICommand<Result<BankAccountResponse>>;
