namespace ZARI.Application.Features.Accounting.ExchangeRates.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateExchangeRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateExchangeRateCommand>
{
    public async Task<Result> HandleAsync(UpdateExchangeRateCommand command, CancellationToken cancellationToken = default)
    {
        var exchangeRate = await dbContext.ExchangeRates.FindAsync([command.Id], cancellationToken);
        if (exchangeRate is null)
            return Result.Failure(Error.NotFound("ExchangeRate.NotFound", $"Exchange rate with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("EXCHANGE_RATES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("ExchangeRate.Forbidden", "You do not have permission to update exchange rates."));

        var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
        if (!currencyExists)
            return Result.Failure(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));

        exchangeRate.CurrencyId = command.CurrencyId;
        exchangeRate.RateDate = command.RateDate;
        exchangeRate.RateToBase = command.RateToBase;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
