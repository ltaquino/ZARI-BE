namespace ZARI.Application.Features.Accounting.GlAccounts.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Domain.Common;

public sealed class GetAllGlAccountsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllGlAccountsQuery, Result<List<GlAccountResponse>>>
{
    public async Task<Result<List<GlAccountResponse>>> HandleAsync(GetAllGlAccountsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.GlAccounts
            .OrderBy(a => a.Code)
            .Select(a => new GlAccountResponse(a.Id, a.Code, a.Name, a.AccountType, a.NormalBalance, a.ParentAccountId, a.Status, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
