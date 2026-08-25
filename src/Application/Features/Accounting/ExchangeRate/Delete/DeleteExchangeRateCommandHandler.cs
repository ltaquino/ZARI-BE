namespace ZARI.Application.Features.Accounting.ExchangeRates.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteExchangeRateCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteExchangeRateCommand>
{
    public async Task<Result> HandleAsync(DeleteExchangeRateCommand command, CancellationToken cancellationToken = default)
    {
        var exchangeRate = await dbContext.ExchangeRates.FindAsync([command.Id], cancellationToken);
        if (exchangeRate is null)
            return Result.Failure(Error.NotFound("ExchangeRate.NotFound", $"Exchange rate with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("EXCHANGE_RATES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("ExchangeRate.Forbidden", "You do not have permission to delete exchange rates."));

        dbContext.ExchangeRates.Remove(exchangeRate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
