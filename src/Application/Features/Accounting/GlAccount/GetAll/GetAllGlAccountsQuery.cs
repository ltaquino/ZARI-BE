namespace ZARI.Application.Features.Accounting.GlAccounts.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Domain.Common;

public sealed record GetAllGlAccountsQuery : IQuery<Result<List<GlAccountResponse>>>;
