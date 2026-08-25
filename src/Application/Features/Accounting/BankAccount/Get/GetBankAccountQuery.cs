namespace ZARI.Application.Features.Accounting.BankAccounts.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetBankAccountQuery(Guid Id) : IQuery<Result<BankAccountResponse>>;

public sealed record BankAccountResponse(Guid Id, string BranchId, string AccountName, string AccountNumber, string BankName, Guid GlAccountId, string? CurrencyId, DateTimeOffset CreatedAt);
