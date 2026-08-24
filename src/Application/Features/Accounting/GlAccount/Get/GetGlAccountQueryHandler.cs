namespace ZARI.Application.Features.Accounting.GlAccounts.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetGlAccountQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetGlAccountQuery, Result<GlAccountResponse>>
{
    public async Task<Result<GlAccountResponse>> HandleAsync(GetGlAccountQuery query, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.GlAccounts
            .Where(a => a.Id == query.Id)
            .Select(a => new GlAccountResponse(a.Id, a.Code, a.Name, a.AccountType, a.NormalBalance, a.ParentAccountId, a.Status, a.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result.Failure<GlAccountResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{query.Id}' was not found."));

        return Result.Success(account);
    }
}
