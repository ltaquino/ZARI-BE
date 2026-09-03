namespace ZARI.Application.Features.Accounting.ExchangeRates.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Domain.Common;

public sealed class GetAllExchangeRatesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllExchangeRatesQuery, Result<List<ExchangeRateResponse>>>
{
    public async Task<Result<List<ExchangeRateResponse>>> HandleAsync(GetAllExchangeRatesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EXCHANGE_RATES", FormAction.View, cancellationToken))
            return Result.Failure<List<ExchangeRateResponse>>(Error.Forbidden("ExchangeRate.Forbidden", "You do not have permission to view exchange rates."));

        var items = await dbContext.ExchangeRates.AsNoTracking()
            .OrderByDescending(e => e.RateDate)
            .Select(e => new ExchangeRateResponse(e.Id, e.CurrencyId, e.RateDate, e.RateToBase, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
