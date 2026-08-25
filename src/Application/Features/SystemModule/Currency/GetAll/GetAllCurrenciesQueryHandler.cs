namespace ZARI.Application.Features.SystemModule.Currencies.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Domain.Common;

public sealed class GetAllCurrenciesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllCurrenciesQuery, Result<List<CurrencyResponse>>>
{
    public async Task<Result<List<CurrencyResponse>>> HandleAsync(GetAllCurrenciesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CURRENCIES", FormAction.View, cancellationToken))
            return Result.Failure<List<CurrencyResponse>>(Error.Forbidden("Currency.Forbidden", "You do not have permission to view currencies."));

        var items = await dbContext.Currencies
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyResponse(c.Id, c.Code, c.Name, c.Status, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
