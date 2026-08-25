namespace ZARI.Application.Features.Accounting.BankAccounts.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.BankAccounts.Get;
using ZARI.Domain.Common;

public sealed record GetAllBankAccountsQuery : IQuery<Result<List<BankAccountResponse>>>;
