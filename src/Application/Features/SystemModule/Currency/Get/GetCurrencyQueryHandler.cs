namespace ZARI.Application.Features.SystemModule.Currencies.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCurrencyQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetCurrencyQuery, Result<CurrencyResponse>>
{
    public async Task<Result<CurrencyResponse>> HandleAsync(GetCurrencyQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CURRENCIES", FormAction.View, cancellationToken))
            return Result.Failure<CurrencyResponse>(Error.Forbidden("Currency.Forbidden", "You do not have permission to view currencies."));

        var currency = await dbContext.Currencies
            .Where(c => c.Id == query.Id)
            .Select(c => new CurrencyResponse(c.Id, c.Code, c.Name, c.Status, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (currency is null)
            return Result.Failure<CurrencyResponse>(Error.NotFound("Currency.NotFound", $"Currency with ID '{query.Id}' was not found."));

        return Result.Success(currency);
    }
}
