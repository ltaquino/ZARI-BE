namespace ZARI.Application.Features.Accounting.GlAccounts.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Domain.Common;

public sealed record CreateGlAccountCommand(
    string Code,
    string Name,
    string AccountType,
    string NormalBalance,
    Guid? ParentAccountId,
    string Status) : ICommand<Result<GlAccountResponse>>;
