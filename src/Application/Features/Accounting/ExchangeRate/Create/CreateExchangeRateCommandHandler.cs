namespace ZARI.Application.Features.Accounting.ExchangeRates.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateExchangeRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateExchangeRateCommand, Result<ExchangeRateResponse>>
{
    public async Task<Result<ExchangeRateResponse>> HandleAsync(CreateExchangeRateCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("EXCHANGE_RATES", FormAction.Create, cancellationToken))
            return Result.Failure<ExchangeRateResponse>(Error.Forbidden("ExchangeRate.Forbidden", "You do not have permission to create exchange rates."));

        var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
        if (!currencyExists)
            return Result.Failure<ExchangeRateResponse>(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));

        var exchangeRate = new ExchangeRate
        {
            CurrencyId = command.CurrencyId,
            RateDate = command.RateDate,
            RateToBase = command.RateToBase
        };

        dbContext.ExchangeRates.Add(exchangeRate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ExchangeRateResponse(exchangeRate.Id, exchangeRate.CurrencyId, exchangeRate.RateDate, exchangeRate.RateToBase, exchangeRate.CreatedAt);
        return Result.Success(response);
    }
}
