namespace ZARI.Application.Features.Accounting.GlAccounts.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetGlAccountQuery(Guid Id) : IQuery<Result<GlAccountResponse>>;

public sealed record GlAccountResponse(
    Guid Id,
    string Code,
    string Name,
    string AccountType,
    string NormalBalance,
    Guid? ParentAccountId,
    string Status,
    DateTimeOffset CreatedAt);
