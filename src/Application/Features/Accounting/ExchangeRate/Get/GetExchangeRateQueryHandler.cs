namespace ZARI.Application.Features.Accounting.ExchangeRates.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetExchangeRateQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetExchangeRateQuery, Result<ExchangeRateResponse>>
{
    public async Task<Result<ExchangeRateResponse>> HandleAsync(GetExchangeRateQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EXCHANGE_RATES", FormAction.View, cancellationToken))
            return Result.Failure<ExchangeRateResponse>(Error.Forbidden("ExchangeRate.Forbidden", "You do not have permission to view exchange rates."));

        var exchangeRate = await dbContext.ExchangeRates
            .Where(e => e.Id == query.Id)
            .Select(e => new ExchangeRateResponse(e.Id, e.CurrencyId, e.RateDate, e.RateToBase, e.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (exchangeRate is null)
            return Result.Failure<ExchangeRateResponse>(Error.NotFound("ExchangeRate.NotFound", $"Exchange rate with ID '{query.Id}' was not found."));

        return Result.Success(exchangeRate);
    }
}
